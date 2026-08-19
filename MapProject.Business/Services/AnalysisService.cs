using MapProject.Business.Dtos;
using MapProject.Business.Geo;
using MapProject.Business.GeoServer;
using MapProject.Business.Settings;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;

namespace MapProject.Business.Services;

public class AnalysisService : IAnalysisService
{
    private readonly IGeoServerFeatureReader _geoServer;
    private readonly GeoServerSettings _settings;

    public AnalysisService(IGeoServerFeatureReader geoServer, IOptions<GeoServerSettings> settings)
    {
        _geoServer = geoServer;
        _settings = settings.Value;
    }

    /// <summary>
    /// Çizilen poligonla kesişen envanteri sayar.
    ///
    /// Kesişim testi eskiden EF üzerinden PostGIS'in ST_Intersects'ine
    /// çevriliyordu; artık aynı işi GeoServer'a CQL uzamsal filtresiyle
    /// yaptırıyoruz. Sonuçta sorgu yine PostGIS'te çalışıyor - arada
    /// GeoServer var, tüm envanter belleğe çekilmiyor.
    ///
    /// INTERSECTS "tamamen içinde" değil "değiyor" demek: ödevde istendiği
    /// gibi kısmi kesişim de sayılıyor.
    /// </summary>
    public async Task<AnalysisResultDto> IntersectAsync(AnalysisRequestDto request, int userId)
    {
        // Metni önce doğrulanmış bir geometriye çevirip sonra AsText() ile
        // yeniden yazıyoruz. Bu bilinçli: istemciden gelen ham metin doğrudan
        // CQL'e gömülseydi filtreye istediğini yazabilirdi. Parse'tan geçen
        // metin artık bizim ürettiğimiz, sadece geometri içeren bir metin.
        var area = WktParser.Parse<Polygon>(request.Wkt, "POLYGON");
        var intersects = $"INTERSECTS({IGeoServerFeatureReader.GeometryColumn}, {area.AsText()})";

        // Kayıtlı bir poligonun analizinde poligonun kendisi hariç tutulur;
        // yoksa kendisiyle kesişip sonucu bir fazla gösterirdi.
        var polygonFilter = request.ExcludePolygonId is { } excludeId
            ? $"{intersects} AND id <> {excludeId}"
            : intersects;

        // Üç katman birbirinden bağımsız; paralel soruyoruz.
        var pointTask = _geoServer.GetOwnedFeaturesAsync(_settings.PointLayer, userId, intersects);
        var lineTask = _geoServer.GetOwnedFeaturesAsync(_settings.LineLayer, userId, intersects);
        var polygonTask = _geoServer.GetOwnedFeaturesAsync(_settings.PolygonLayer, userId, polygonFilter);

        await Task.WhenAll(pointTask, lineTask, polygonTask);

        var points = ToItems(await pointTask, "point");
        var lines = ToItems(await lineTask, "line");
        var polygons = ToItems(await polygonTask, "polygon");

        return new AnalysisResultDto
        {
            PointCount = points.Count,
            LineCount = lines.Count,
            PolygonCount = polygons.Count,
            TotalCount = points.Count + lines.Count + polygons.Count,
            Items = [.. points, .. lines, .. polygons]
        };
    }

    private static List<AnalysisItemDto> ToItems(IReadOnlyList<FeatureDto> features, string type) =>
        features
            .Select(f => new AnalysisItemDto { Type = type, Id = f.Id, Name = f.Name })
            .ToList();
}
