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

    public async Task<FeatureCollectionDto> GetAllAsync()
    {
        // Geometriyi WKT'ye çevirmek .NET tarafında olmalı, bu yüzden önce
        // satırları çekip sonra map'liyoruz.
        var points = await _context.Points.AsNoTracking().OrderBy(p => p.Id).ToListAsync();
        var lines = await _context.Lines.AsNoTracking().OrderBy(l => l.Id).ToListAsync();
        var polygons = await _context.Polygons.AsNoTracking().OrderBy(p => p.Id).ToListAsync();

        return new FeatureCollectionDto
        {
            Points = points.Select(p => ToDto(p.Id, p.Name, p.Geometry, p.Color, p.CreatedDate)).ToList(),
            Lines = lines.Select(l => ToDto(l.Id, l.Name, l.Geometry, l.Color, l.CreatedDate)).ToList(),
            Polygons = polygons.Select(p => ToDto(p.Id, p.Name, p.Geometry, p.Color, p.CreatedDate)).ToList()
        };
    }

    public async Task<FeatureDto> CreatePointAsync(FeatureCreateDto dto)
    {
        var entity = new PointFeature
        {
            Name = dto.Name,
            Color = dto.Color,
            Geometry = WktParser.Parse<Point>(dto.Wkt, "POINT"),
            CreatedDate = DateTime.UtcNow
        };

        _context.Points.Add(entity);
        await _context.SaveChangesAsync();

        return ToDto(entity.Id, entity.Name, entity.Geometry, entity.Color, entity.CreatedDate);
    }

    public async Task<FeatureDto> CreateLineAsync(FeatureCreateDto dto)
    {
        var entity = new LineFeature
        {
            Name = dto.Name,
            Color = dto.Color,
            Geometry = WktParser.Parse<LineString>(dto.Wkt, "LINESTRING"),
            CreatedDate = DateTime.UtcNow
        };

        _context.Lines.Add(entity);
        await _context.SaveChangesAsync();

        return ToDto(entity.Id, entity.Name, entity.Geometry, entity.Color, entity.CreatedDate);
    }

    public async Task<FeatureDto> CreatePolygonAsync(FeatureCreateDto dto)
    {
        var entity = new PolygonFeature
        {
            Name = dto.Name,
            Color = dto.Color,
            Geometry = WktParser.Parse<Polygon>(dto.Wkt, "POLYGON"),
            CreatedDate = DateTime.UtcNow
        };

        _context.Polygons.Add(entity);
        await _context.SaveChangesAsync();

        return ToDto(entity.Id, entity.Name, entity.Geometry, entity.Color, entity.CreatedDate);
    }

    public Task<bool> DeletePointAsync(int id) => DeleteAsync(_context.Points, id);

    public Task<bool> DeleteLineAsync(int id) => DeleteAsync(_context.Lines, id);

    public Task<bool> DeletePolygonAsync(int id) => DeleteAsync(_context.Polygons, id);

    /// <summary>
    /// Üç tablo için ortak silme. Kaydı önce çekiyoruz: yoksa controller'ın
    /// 404 dönebilmesi için bunu bilmesi gerekiyor.
    /// </summary>
    private async Task<bool> DeleteAsync<TEntity>(DbSet<TEntity> set, int id)
        where TEntity : class
    {
        var entity = await set.FindAsync(id);

        if (entity is null)
        {
            return false;
        }

        set.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    private static FeatureDto ToDto(int id, string name, Geometry geometry, string color, DateTime createdDate) =>
        new()
        {
            Id = id,
            Name = name,
            // AsText() geometriyi WKT metnine çevirir.
            Wkt = geometry.AsText(),
            Color = color,
            CreatedDate = createdDate
        };
}
