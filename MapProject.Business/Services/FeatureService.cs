using MapProject.Business.Dtos;
using MapProject.Data;
using MapProject.Entities;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace MapProject.Business.Services;

public class FeatureService : IFeatureService
{
    /// <summary>Veritabanı tarafındaki koordinat sistemi: WGS84, derece.</summary>
    private const int DatabaseSrid = 4326;

    // Varsayılan ayarları alıp sadece SRID'yi 4326 yapıyoruz: WKTReader
    // ürettiği geometrilere bu SRID'yi damgalar. Varsayılanla bıraksaydık
    // SRID 0 olurdu ve geometry(Point,4326) kolonu kaydı reddederdi.
    private static readonly NtsGeometryServices GeometryServices = new(
        NtsGeometryServices.Instance.DefaultCoordinateSequenceFactory,
        NtsGeometryServices.Instance.DefaultPrecisionModel,
        DatabaseSrid);

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
            Points = points.Select(p => ToDto(p.Id, p.Name, p.Geometry, p.CreatedDate)).ToList(),
            Lines = lines.Select(l => ToDto(l.Id, l.Name, l.Geometry, l.CreatedDate)).ToList(),
            Polygons = polygons.Select(p => ToDto(p.Id, p.Name, p.Geometry, p.CreatedDate)).ToList()
        };
    }

    public async Task<FeatureDto> CreatePointAsync(FeatureCreateDto dto)
    {
        var entity = new PointFeature
        {
            Name = dto.Name,
            Geometry = ParseWkt<Point>(dto.Wkt, "POINT"),
            CreatedDate = DateTime.UtcNow
        };

        _context.Points.Add(entity);
        await _context.SaveChangesAsync();

        return ToDto(entity.Id, entity.Name, entity.Geometry, entity.CreatedDate);
    }

    public async Task<FeatureDto> CreateLineAsync(FeatureCreateDto dto)
    {
        var entity = new LineFeature
        {
            Name = dto.Name,
            Geometry = ParseWkt<LineString>(dto.Wkt, "LINESTRING"),
            CreatedDate = DateTime.UtcNow
        };

        _context.Lines.Add(entity);
        await _context.SaveChangesAsync();

        return ToDto(entity.Id, entity.Name, entity.Geometry, entity.CreatedDate);
    }

    public async Task<FeatureDto> CreatePolygonAsync(FeatureCreateDto dto)
    {
        var entity = new PolygonFeature
        {
            Name = dto.Name,
            Geometry = ParseWkt<Polygon>(dto.Wkt, "POLYGON"),
            CreatedDate = DateTime.UtcNow
        };

        _context.Polygons.Add(entity);
        await _context.SaveChangesAsync();

        return ToDto(entity.Id, entity.Name, entity.Geometry, entity.CreatedDate);
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

    private static FeatureDto ToDto(int id, string name, Geometry geometry, DateTime createdDate) =>
        new()
        {
            Id = id,
            Name = name,
            // AsText() geometriyi WKT metnine çevirir.
            Wkt = geometry.AsText(),
            CreatedDate = createdDate
        };

    /// <summary>
    /// WKT metnini istenen geometri tipine çevirir.
    /// İstemciden gelen metne güvenmiyoruz: bozuk olabilir, ya da
    /// nokta endpoint'ine poligon gönderilmiş olabilir.
    /// </summary>
    private static TGeometry ParseWkt<TGeometry>(string wkt, string expectedType)
        where TGeometry : Geometry
    {
        Geometry parsed;

        try
        {
            parsed = new WKTReader(GeometryServices).Read(wkt);
        }
        catch (Exception ex)
        {
            throw new InvalidGeometryException($"WKT okunamadı: {ex.Message}");
        }

        if (parsed is not TGeometry typed)
        {
            throw new InvalidGeometryException(
                $"Bu uç nokta {expectedType} bekliyor, gelen geometri: {parsed.GeometryType}.");
        }

        if (typed.IsEmpty)
        {
            throw new InvalidGeometryException("Geometri boş olamaz.");
        }

        // Kendini kesen poligon gibi bozuk şekiller PostGIS'te sorun çıkarır.
        if (!typed.IsValid)
        {
            throw new InvalidGeometryException("Geometri geçersiz (ör. kendini kesen poligon).");
        }

        // WKTReader factory'den SRID'yi alır, yine de garantiye alıyoruz.
        typed.SRID = DatabaseSrid;
        return typed;
    }
}
