using MapProject.Business.Dtos;
using MapProject.Business.Geo;
using MapProject.Business.GeoServer;
using MapProject.Business.Settings;
using MapProject.Data;
using MapProject.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;

namespace MapProject.Business.Services;

public class FeatureService : IFeatureService
{
    private readonly AppDbContext _context;
    private readonly IGeoPermissionService _geoPermissionService;
    private readonly IGeoServerFeatureReader _geoServer;
    private readonly GeoServerSettings _geoServerSettings;

    public FeatureService(
        AppDbContext context,
        IGeoPermissionService geoPermissionService,
        IGeoServerFeatureReader geoServer,
        IOptions<GeoServerSettings> geoServerSettings)
    {
        _context = context;
        _geoPermissionService = geoPermissionService;
        _geoServer = geoServer;
        _geoServerSettings = geoServerSettings.Value;
    }

    /// <summary>
    /// Çizim, kullanıcıya tanımlı alanın içinde mi?
    /// Covers kullanıyoruz, Contains değil: sınırın tam üstüne çizilen
    /// nokta da geçerli sayılsın. Contains sınırı dışarıda bırakıyor.
    /// </summary>
    private async Task EnsureInsideAllowedAreaAsync(Geometry geometry, int userId)
    {
        var area = await _geoPermissionService.GetEffectiveAreaAsync(userId);

        // null = tanımlı alan yok = kısıt yok.
        if (area is null) return;

        if (!area.Covers(geometry))
        {
            throw new OutsideAllowedAreaException();
        }
    }

    /// <summary>
    /// Haritayı dolduran okuma. Veriyi artık veritabanından değil
    /// GeoServer'ın WFS servisinden alıyoruz (ödev gereği): istek
    /// React -> bu API -> GeoServer -> PostGIS yolunu izliyor.
    ///
    /// Yazma işlemleri (create/update/delete) EF üzerinden devam ediyor.
    /// Sebebi: kaydetmeden önce coğrafi alan kontrolü, izleme kolonlarının
    /// damgalanması ve soft delete gibi iş kuralları çalışıyor - bunlar
    /// GeoServer'ın değil bizim sorumluluğumuz.
    ///
    /// Üç katman birbirinden bağımsız olduğu için istekleri paralel atıyoruz;
    /// sırayla gitseydi ağ gecikmesi üç katına çıkardı.
    /// </summary>
    public async Task<FeatureCollectionDto> GetAllAsync(int userId)
    {
        var points = _geoServer.GetOwnedFeaturesAsync(_geoServerSettings.PointLayer, userId);
        var lines = _geoServer.GetOwnedFeaturesAsync(_geoServerSettings.LineLayer, userId);
        var polygons = _geoServer.GetOwnedFeaturesAsync(_geoServerSettings.PolygonLayer, userId);

        await Task.WhenAll(points, lines, polygons);

        return new FeatureCollectionDto
        {
            Points = await points,
            Lines = await lines,
            Polygons = await polygons
        };
    }

    // --- Oluşturma ---

    public async Task<FeatureDto> CreatePointAsync(FeatureCreateDto dto, int userId)
    {
        var entity = new PointFeature { Geometry = WktParser.Parse<Point>(dto.Wkt, "POINT") };
        await EnsureInsideAllowedAreaAsync(entity.Geometry, userId);
        return await AddAsync(_context.Points, entity, dto, userId, entity.Geometry);
    }

    public async Task<FeatureDto> CreateLineAsync(FeatureCreateDto dto, int userId)
    {
        var entity = new LineFeature { Geometry = WktParser.Parse<LineString>(dto.Wkt, "LINESTRING") };
        await EnsureInsideAllowedAreaAsync(entity.Geometry, userId);
        return await AddAsync(_context.Lines, entity, dto, userId, entity.Geometry);
    }

    public async Task<FeatureDto> CreatePolygonAsync(FeatureCreateDto dto, int userId)
    {
        var entity = new PolygonFeature { Geometry = WktParser.Parse<Polygon>(dto.Wkt, "POLYGON") };
        await EnsureInsideAllowedAreaAsync(entity.Geometry, userId);
        return await AddAsync(_context.Polygons, entity, dto, userId, entity.Geometry);
    }

    // --- Güncelleme ---

    public async Task<FeatureDto?> UpdatePointAsync(int id, FeatureUpdateDto dto, int userId)
    {
        var entity = await FindOwnedAsync(_context.Points, id, userId);
        if (entity is null) return null;

        entity.Geometry = WktParser.Parse<Point>(dto.Wkt, "POINT");
        await EnsureInsideAllowedAreaAsync(entity.Geometry, userId);
        return await ApplyUpdateAsync(entity, dto, entity.Geometry);
    }

    public async Task<FeatureDto?> UpdateLineAsync(int id, FeatureUpdateDto dto, int userId)
    {
        var entity = await FindOwnedAsync(_context.Lines, id, userId);
        if (entity is null) return null;

        entity.Geometry = WktParser.Parse<LineString>(dto.Wkt, "LINESTRING");
        await EnsureInsideAllowedAreaAsync(entity.Geometry, userId);
        return await ApplyUpdateAsync(entity, dto, entity.Geometry);
    }

    public async Task<FeatureDto?> UpdatePolygonAsync(int id, FeatureUpdateDto dto, int userId)
    {
        var entity = await FindOwnedAsync(_context.Polygons, id, userId);
        if (entity is null) return null;

        entity.Geometry = WktParser.Parse<Polygon>(dto.Wkt, "POLYGON");
        await EnsureInsideAllowedAreaAsync(entity.Geometry, userId);
        return await ApplyUpdateAsync(entity, dto, entity.Geometry);
    }

    // --- Soft delete ---

    public Task<bool> DeletePointAsync(int id, int userId) => SoftDeleteAsync(_context.Points, id, userId);

    public Task<bool> DeleteLineAsync(int id, int userId) => SoftDeleteAsync(_context.Lines, id, userId);

    public Task<bool> DeletePolygonAsync(int id, int userId) => SoftDeleteAsync(_context.Polygons, id, userId);

    // --- Ortak yardımcılar ---

    /// <summary>
    /// Kaydı sahiplik kontrolüyle bulur. Başkasının kaydında da null dönmesi
    /// bilinçli: controller 404 veriyor, böylece "bu id var ama senin değil"
    /// bilgisi sızmıyor.
    /// AsNoTracking YOK - güncelleme için değişikliğin izlenmesi gerekiyor.
    /// </summary>
    private static Task<TEntity?> FindOwnedAsync<TEntity>(DbSet<TEntity> set, int id, int userId)
        where TEntity : class, ITrackable
    {
        return set.FirstOrDefaultAsync(e => e.Id == id && e.InsertedUserId == userId);
    }

    private async Task<FeatureDto> AddAsync<TEntity>(
        DbSet<TEntity> set, TEntity entity, FeatureCreateDto dto, int userId, Geometry geometry)
        where TEntity : class, ITrackable
    {
        entity.Name = dto.Name;
        entity.Color = dto.Color;
        entity.InsertedUserId = userId;
        entity.InsertedDate = DateTime.UtcNow;
        entity.IsActive = true;

        set.Add(entity);
        await _context.SaveChangesAsync();

        return ToDto(entity, geometry);
    }

    private async Task<FeatureDto> ApplyUpdateAsync<TEntity>(
        TEntity entity, FeatureUpdateDto dto, Geometry geometry)
        where TEntity : class, ITrackable
    {
        entity.Name = dto.Name;
        entity.Color = dto.Color;

        if (dto.IsActive.HasValue)
        {
            entity.IsActive = dto.IsActive.Value;
        }

        // modified_date'i elle yazmıyoruz; AppDbContext.SaveChanges damgalıyor.
        await _context.SaveChangesAsync();

        return ToDto(entity, geometry);
    }

    private async Task<bool> SoftDeleteAsync<TEntity>(DbSet<TEntity> set, int id, int userId)
        where TEntity : class, ITrackable
    {
        var entity = await FindOwnedAsync(set, id, userId);

        if (entity is null)
        {
            return false;
        }

        // Satır veritabanında kalıyor; global sorgu filtresi bundan sonra gizliyor.
        entity.IsDeleted = true;
        await _context.SaveChangesAsync();
        return true;
    }

    private static FeatureDto ToDto(ITrackable entity, Geometry geometry) =>
        new()
        {
            Id = entity.Id,
            Name = entity.Name,
            // AsText() geometriyi WKT metnine çevirir.
            Wkt = geometry.AsText(),
            Color = entity.Color,
            InsertedUserId = entity.InsertedUserId,
            InsertedDate = entity.InsertedDate,
            ModifiedDate = entity.ModifiedDate,
            IsActive = entity.IsActive
        };
}
