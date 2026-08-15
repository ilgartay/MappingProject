using MapProject.Business.Dtos;
using MapProject.Business.Geo;
using MapProject.Data;
using MapProject.Entities;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace MapProject.Business.Services;

public class FeatureService : IFeatureService
{
    private readonly AppDbContext _context;

    public FeatureService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<FeatureCollectionDto> GetAllAsync(int userId)
    {
        // Silinmiş kayıtları global sorgu filtresi zaten eliyor;
        // burada sadece sahiplik filtresi var.
        // Geometriyi WKT'ye çevirmek .NET tarafında olmalı, bu yüzden önce
        // satırları çekip sonra map'liyoruz.
        var points = await OwnedQuery(_context.Points, userId).ToListAsync();
        var lines = await OwnedQuery(_context.Lines, userId).ToListAsync();
        var polygons = await OwnedQuery(_context.Polygons, userId).ToListAsync();

        return new FeatureCollectionDto
        {
            Points = points.Select(p => ToDto(p, p.Geometry)).ToList(),
            Lines = lines.Select(l => ToDto(l, l.Geometry)).ToList(),
            Polygons = polygons.Select(p => ToDto(p, p.Geometry)).ToList()
        };
    }

    // --- Oluşturma ---

    public async Task<FeatureDto> CreatePointAsync(FeatureCreateDto dto, int userId)
    {
        var entity = new PointFeature { Geometry = WktParser.Parse<Point>(dto.Wkt, "POINT") };
        return await AddAsync(_context.Points, entity, dto, userId, entity.Geometry);
    }

    public async Task<FeatureDto> CreateLineAsync(FeatureCreateDto dto, int userId)
    {
        var entity = new LineFeature { Geometry = WktParser.Parse<LineString>(dto.Wkt, "LINESTRING") };
        return await AddAsync(_context.Lines, entity, dto, userId, entity.Geometry);
    }

    public async Task<FeatureDto> CreatePolygonAsync(FeatureCreateDto dto, int userId)
    {
        var entity = new PolygonFeature { Geometry = WktParser.Parse<Polygon>(dto.Wkt, "POLYGON") };
        return await AddAsync(_context.Polygons, entity, dto, userId, entity.Geometry);
    }

    // --- Güncelleme ---

    public async Task<FeatureDto?> UpdatePointAsync(int id, FeatureUpdateDto dto, int userId)
    {
        var entity = await FindOwnedAsync(_context.Points, id, userId);
        if (entity is null) return null;

        entity.Geometry = WktParser.Parse<Point>(dto.Wkt, "POINT");
        return await ApplyUpdateAsync(entity, dto, entity.Geometry);
    }

    public async Task<FeatureDto?> UpdateLineAsync(int id, FeatureUpdateDto dto, int userId)
    {
        var entity = await FindOwnedAsync(_context.Lines, id, userId);
        if (entity is null) return null;

        entity.Geometry = WktParser.Parse<LineString>(dto.Wkt, "LINESTRING");
        return await ApplyUpdateAsync(entity, dto, entity.Geometry);
    }

    public async Task<FeatureDto?> UpdatePolygonAsync(int id, FeatureUpdateDto dto, int userId)
    {
        var entity = await FindOwnedAsync(_context.Polygons, id, userId);
        if (entity is null) return null;

        entity.Geometry = WktParser.Parse<Polygon>(dto.Wkt, "POLYGON");
        return await ApplyUpdateAsync(entity, dto, entity.Geometry);
    }

    // --- Soft delete ---

    public Task<bool> DeletePointAsync(int id, int userId) => SoftDeleteAsync(_context.Points, id, userId);

    public Task<bool> DeleteLineAsync(int id, int userId) => SoftDeleteAsync(_context.Lines, id, userId);

    public Task<bool> DeletePolygonAsync(int id, int userId) => SoftDeleteAsync(_context.Polygons, id, userId);

    // --- Ortak yardımcılar ---

    private static IQueryable<TEntity> OwnedQuery<TEntity>(DbSet<TEntity> set, int userId)
        where TEntity : class, ITrackable
    {
        return set.AsNoTracking()
            .Where(e => e.InsertedUserId == userId)
            .OrderBy(e => e.Id);
    }

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
