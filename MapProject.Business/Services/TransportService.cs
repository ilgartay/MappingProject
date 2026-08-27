using MapProject.Business.Dtos;
using MapProject.Business.Exceptions;
using MapProject.Business.Geo;
using MapProject.Data;
using MapProject.Entities;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace MapProject.Business.Services;

public class TransportService : ITransportService
{
    private readonly AppDbContext _context;

    public TransportService(AppDbContext context)
    {
        _context = context;
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
        return await GetRouteAsync(routeId);
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
