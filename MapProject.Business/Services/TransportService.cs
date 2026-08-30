using MapProject.Business.Dtos;
using MapProject.Business.Exceptions;
using MapProject.Business.Geo;
using MapProject.Business.Routing;
using MapProject.Data;
using MapProject.Entities;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace MapProject.Business.Services;

public class TransportService : ITransportService
{
    private readonly AppDbContext _context;
    private readonly IOsrmClient _osrm;

    public TransportService(AppDbContext context, IOsrmClient osrm)
    {
        _context = context;
        _osrm = osrm;
    }

    // --- Güzergah ---

    public async Task<IReadOnlyList<RouteDto>> GetRoutesAsync()
    {
        // Geometriyi WKT'ye çevirmek .NET tarafında olmalı; önce satırları
        // çekip sonra map'liyoruz.
        var routes = await LoadRoutesQuery().ToListAsync();
        return routes.Select(ToDto).ToList();
    }

    public async Task<RouteDto?> GetRouteAsync(int id)
    {
        var route = await LoadRoutesQuery().FirstOrDefaultAsync(r => r.Id == id);
        return route is null ? null : ToDto(route);
    }

    public async Task<RouteDto> CreateRouteAsync(RouteSaveDto dto)
    {
        await EnsureRouteNameIsFreeAsync(dto.Name, null);

        var route = new Route
        {
            Name = dto.Name.Trim(),
            Color = dto.Color,
            IsActive = dto.IsActive,
            CreatedDate = DateTime.UtcNow
        };

        _context.Routes.Add(route);
        await _context.SaveChangesAsync();

        return (await GetRouteAsync(route.Id))!;
    }

    public async Task<RouteDto?> UpdateRouteAsync(int id, RouteSaveDto dto)
    {
        var route = await _context.Routes.FirstOrDefaultAsync(r => r.Id == id);

        if (route is null)
        {
            return null;
        }

        await EnsureRouteNameIsFreeAsync(dto.Name, id);

        route.Name = dto.Name.Trim();
        route.Color = dto.Color;
        route.IsActive = dto.IsActive;

        // modified_date'i elle yazmıyoruz; AppDbContext.SaveChanges damgalıyor.
        await _context.SaveChangesAsync();

        return await GetRouteAsync(id);
    }

    public async Task<bool> DeleteRouteAsync(int id)
    {
        var route = await _context.Routes.FirstOrDefaultAsync(r => r.Id == id);

        if (route is null)
        {
            return false;
        }

        // Durakları sessizce silmiyoruz: durak bir güzergaha ait olmak
        // zorunda, "sahipsiz durak" diye bir durum yok. Kullanıcı neyi
        // kaybedeceğini bilerek karar versin.
        var stopCount = await _context.Stops.CountAsync(s => s.RouteId == id);

        if (stopCount > 0)
        {
            throw new InvalidUserOperationException(
                $"Bu güzergahta {stopCount} durak var. Önce durakları silin.");
        }

        route.IsDeleted = true;
        await _context.SaveChangesAsync();
        return true;
    }

    // --- Rota (OSRM) ---

    public async Task<RouteDto> BuildRouteAsync(int routeId, CancellationToken cancellationToken = default)
    {
        var route = await TrackRouteAsync(routeId)
            ?? throw new InvalidUserOperationException("Seçilen güzergah bulunamadı.");

        await ApplyOsrmRouteAsync(route, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return (await GetRouteAsync(routeId))!;
    }

    // --- Durak ---

    public async Task<StopDto> CreateStopAsync(StopSaveDto dto)
    {
        var route = await FindRouteAsync(dto.RouteId);
        var geometry = WktParser.Parse<Point>(dto.Wkt, "POINT");

        // Yeni durak hattın sonuna ekleniyor; sırayı kullanıcı sonradan
        // sürükle-bırak ile değiştiriyor.
        var lastOrder = await _context.Stops
            .Where(s => s.RouteId == route.Id)
            .MaxAsync(s => (int?)s.Order) ?? 0;

        var stop = new Stop
        {
            Name = dto.Name.Trim(),
            RouteId = route.Id,
            Geometry = geometry,
            Order = lastOrder + 1,
            IsActive = dto.IsActive,
            CreatedDate = DateTime.UtcNow
        };

        _context.Stops.Add(stop);
        await _context.SaveChangesAsync();

        await RefreshRouteAsync(route.Id);
        return ToDto(stop, route);
    }

    public async Task<StopDto?> UpdateStopAsync(int id, StopSaveDto dto)
    {
        var stop = await _context.Stops.FirstOrDefaultAsync(s => s.Id == id);

        if (stop is null)
        {
            return null;
        }

        var route = await FindRouteAsync(dto.RouteId);
        var previousRouteId = stop.RouteId;

        // Durak başka güzergaha taşınıyorsa yeni hattın sonuna geçiyor:
        // eski sıra numarası yeni hatta bir başkasıyla çakışabilirdi.
        if (stop.RouteId != route.Id)
        {
            var lastOrder = await _context.Stops
                .Where(s => s.RouteId == route.Id)
                .MaxAsync(s => (int?)s.Order) ?? 0;

            stop.RouteId = route.Id;
            stop.Order = lastOrder + 1;
        }

        stop.Name = dto.Name.Trim();
        stop.Geometry = WktParser.Parse<Point>(dto.Wkt, "POINT");
        stop.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();

        // Durak başka hatta taşındıysa iki hattın da rotası bozuldu.
        await RefreshRouteAsync(route.Id);

        if (previousRouteId != route.Id)
        {
            await RefreshRouteAsync(previousRouteId);
        }

        return ToDto(stop, route);
    }

    public async Task<bool> DeleteStopAsync(int id)
    {
        var stop = await _context.Stops.FirstOrDefaultAsync(s => s.Id == id);

        if (stop is null)
        {
            return false;
        }

        stop.IsDeleted = true;
        await _context.SaveChangesAsync();

        // Kalan durakların sırasında boşluk kalmasın (1,2,4 -> 1,2,3).
        await RenumberAsync(stop.RouteId);
        await RefreshRouteAsync(stop.RouteId);
        return true;
    }

    public async Task<RouteDto?> ReorderStopsAsync(int routeId, StopOrderDto dto)
    {
        var stops = await _context.Stops
            .Where(s => s.RouteId == routeId)
            .ToListAsync();

        if (stops.Count == 0)
        {
            return await GetRouteAsync(routeId);
        }

        // Gelen liste güzergahın duraklarıyla birebir örtüşmeli. Eksik ya
        // da yabancı id gelirse sıralamayı hiç uygulamıyoruz: yarım
        // uygulanmış bir sıra, sürükle-bırak öncesinden de kötü olurdu.
        var incoming = dto.StopIds.Distinct().ToList();

        if (incoming.Count != stops.Count || incoming.Any(id => stops.All(s => s.Id != id)))
        {
            throw new InvalidUserOperationException(
                "Sıralama listesi güzergahın duraklarıyla eşleşmiyor.");
        }

        for (var i = 0; i < incoming.Count; i++)
        {
            stops.First(s => s.Id == incoming[i]).Order = i + 1;
        }

        await _context.SaveChangesAsync();

        // Ödevin istediği otomatik güncelleme burada: sıra değişti, rota
        // artık duraklara uymuyor, OSRM'e yeni istek gidiyor.
        var warning = await RefreshRouteAsync(routeId);

        var result = await GetRouteAsync(routeId);

        if (result is not null)
        {
            result.RouteWarning = warning;
        }

        return result;
    }

    // --- Yardımcılar ---

    private IQueryable<Route> LoadRoutesQuery()
    {
        return _context.Routes
            .AsNoTracking()
            .Include(r => r.Stops.OrderBy(s => s.Order))
            .OrderBy(r => r.Name);
    }

    private async Task<Route> FindRouteAsync(int id)
    {
        var route = await _context.Routes.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);

        if (route is null)
        {
            throw new InvalidUserOperationException("Seçilen güzergah bulunamadı.");
        }

        if (!route.IsActive)
        {
            throw new InvalidUserOperationException($"'{route.Name}' güzergahı pasif durumda.");
        }

        return route;
    }

    private async Task EnsureRouteNameIsFreeAsync(string name, int? excludeId)
    {
        var trimmed = name.Trim();

        var exists = await _context.Routes.AnyAsync(r =>
            r.Name == trimmed && (excludeId == null || r.Id != excludeId));

        if (exists)
        {
            throw new InvalidUserOperationException($"'{trimmed}' adında bir güzergah zaten var.");
        }
    }

    /// <summary>Değiştirmek üzere güzergahı duraklarıyla birlikte okur.</summary>
    private Task<Route?> TrackRouteAsync(int routeId) =>
        _context.Routes
            .Include(r => r.Stops.OrderBy(s => s.Order))
            .FirstOrDefaultAsync(r => r.Id == routeId);

    /// <summary>
    /// Durakları OSRM'e verip dönen çizgiyi güzergaha yazar.
    /// SaveChanges çağıran tarafta.
    /// </summary>
    private async Task ApplyOsrmRouteAsync(Route route, CancellationToken cancellationToken)
    {
        var waypoints = route.Stops
            .OrderBy(s => s.Order)
            .Select(s => s.Geometry.Coordinate)
            .ToList();

        if (waypoints.Count < 2)
        {
            throw new InvalidUserOperationException(
                "Rota oluşturmak için güzergahta en az iki durak olmalı.");
        }

        var result = await _osrm.GetRouteAsync(waypoints, cancellationToken);

        route.RouteGeometry = result.Geometry;
        route.RouteDistance = result.DistanceMeters;
        route.RouteDuration = result.DurationSeconds;
        route.RouteBuiltAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Duraklar değiştikten sonra rotayı yeniden hesaplar. Sorun çıkarsa
    /// açıklama metnini döndürür, yoksa null.
    ///
    /// Yalnızca daha önce rotası üretilmiş güzergahlara dokunuyor: hiç
    /// "Rota Oluştur" denmemiş bir hatta durak eklemek OSRM'e istek
    /// atmamalı, kullanıcı henüz rota istemedi.
    ///
    /// Hesaplama başarısızsa eski çizgiyi SİLİYORUZ. Bırakmak daha
    /// zararsız görünüyor ama değil: çizgi artık durak sırasına uymuyor
    /// ve haritada yanlış bir rotayı doğruymuş gibi gösterirdi. Boş
    /// harita "rota yok" der, yanlış rota ise yalan söyler.
    /// </summary>
    private async Task<string?> RefreshRouteAsync(int routeId)
    {
        var route = await TrackRouteAsync(routeId);

        if (route?.RouteGeometry is null)
        {
            return null;
        }

        try
        {
            await ApplyOsrmRouteAsync(route, CancellationToken.None);
        }
        catch (Exception ex) when (ex is OsrmException or InvalidUserOperationException)
        {
            route.RouteGeometry = null;
            route.RouteDistance = null;
            route.RouteDuration = null;
            route.RouteBuiltAt = null;
            await _context.SaveChangesAsync();

            return $"Durak sırası kaydedildi ama rota güncellenemedi: {ex.Message}";
        }

        await _context.SaveChangesAsync();
        return null;
    }

    /// <summary>Silme sonrası sıra numaralarını 1'den yeniden dizer.</summary>
    private async Task RenumberAsync(int routeId)
    {
        var stops = await _context.Stops
            .Where(s => s.RouteId == routeId)
            .OrderBy(s => s.Order)
            .ToListAsync();

        for (var i = 0; i < stops.Count; i++)
        {
            stops[i].Order = i + 1;
        }

        await _context.SaveChangesAsync();
    }

    private static RouteDto ToDto(Route route) =>
        new()
        {
            Id = route.Id,
            Name = route.Name,
            Color = route.Color,
            IsActive = route.IsActive,
            CreatedDate = route.CreatedDate,
            ModifiedDate = route.ModifiedDate,
            RouteWkt = route.RouteGeometry?.AsText(),
            RouteDistance = route.RouteDistance,
            RouteDuration = route.RouteDuration,
            RouteBuiltAt = route.RouteBuiltAt,
            Stops = route.Stops.Select(s => ToDto(s, route)).ToList()
        };

    private static StopDto ToDto(Stop stop, Route route) =>
        new()
        {
            Id = stop.Id,
            Name = stop.Name,
            Wkt = stop.Geometry.AsText(),
            RouteId = route.Id,
            RouteName = route.Name,
            RouteColor = route.Color,
            Order = stop.Order,
            IsActive = stop.IsActive,
            CreatedDate = stop.CreatedDate,
            ModifiedDate = stop.ModifiedDate
        };
}
