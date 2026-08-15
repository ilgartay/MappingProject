using MapProject.Business.Dtos;
using MapProject.Business.Geo;
using MapProject.Data;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace MapProject.Business.Services;

public class AnalysisService : IAnalysisService
{
    private readonly AppDbContext _context;

    public AnalysisService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AnalysisResultDto> IntersectAsync(AnalysisRequestDto request, int userId)
    {
        var area = WktParser.Parse<Polygon>(request.Wkt, "POLYGON");

        // EF Core'un Npgsql sağlayıcısı Intersects çağrısını PostGIS'in
        // ST_Intersects fonksiyonuna çeviriyor; yani filtreleme veritabanında
        // yapılıyor, tüm envanteri belleğe çekmiyoruz.
        //
        // Intersects "tamamen içinde" demek değil, "değiyor" demek: ödevde
        // istendiği gibi ufak bir kesişim de sayılıyor. Tamamen kapsananları
        // istesek Contains/Within kullanmamız gerekirdi.
        var points = await _context.Points
            .AsNoTracking()
            .Where(p => p.InsertedUserId == userId && area.Intersects(p.Geometry))
            .OrderBy(p => p.Id)
            .Select(p => new AnalysisItemDto { Type = "point", Id = p.Id, Name = p.Name })
            .ToListAsync();

        var lines = await _context.Lines
            .AsNoTracking()
            .Where(l => l.InsertedUserId == userId && area.Intersects(l.Geometry))
            .OrderBy(l => l.Id)
            .Select(l => new AnalysisItemDto { Type = "line", Id = l.Id, Name = l.Name })
            .ToListAsync();

        // Kayıtlı bir poligonun analizinde poligonun kendisi hariç tutulur.
        // Sorguya çevrilebilmesi için değeri önce yerel değişkene alıyoruz.
        var excludeId = request.ExcludePolygonId;

        var polygons = await _context.Polygons
            .AsNoTracking()
            .Where(p => p.InsertedUserId == userId && area.Intersects(p.Geometry)
                        && (excludeId == null || p.Id != excludeId))
            .OrderBy(p => p.Id)
            .Select(p => new AnalysisItemDto { Type = "polygon", Id = p.Id, Name = p.Name })
            .ToListAsync();

        return new AnalysisResultDto
        {
            PointCount = points.Count,
            LineCount = lines.Count,
            PolygonCount = polygons.Count,
            TotalCount = points.Count + lines.Count + polygons.Count,
            Items = [.. points, .. lines, .. polygons]
        };
    }
}
