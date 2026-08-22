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

        // Katmanlar birbirinden bağımsız; paralel soruyoruz.
        var pointTask = _geoServer.GetOwnedFeaturesAsync(_settings.PointLayer, userId, intersects);
        var lineTask = _geoServer.GetOwnedFeaturesAsync(_settings.LineLayer, userId, intersects);
        var polygonTask = _geoServer.GetOwnedFeaturesAsync(_settings.PolygonLayer, userId, polygonFilter);

        // POI'de sahiplik filtresi yok: ilgi noktaları paylaşılan veri,
        // alanın içine düşen her POI sayılıyor - kim eklemiş olursa olsun.
        var poiTask = _geoServer.QueryAsync(_settings.PoiLayer, intersects);

        await Task.WhenAll(pointTask, lineTask, polygonTask, poiTask);

        var points = ToItems(await pointTask, "point");
        var lines = ToItems(await lineTask, "line");
        var polygons = ToItems(await polygonTask, "polygon");

        // POI'nin ad kolonu "isim"; diğerlerindeki "name" değil.
        var pois = (await poiTask)
            .Select(r => new AnalysisItemDto { Type = "poi", Id = r.GetInt("id"), Name = r.GetString("isim") })
            .ToList();

        return new AnalysisResultDto
        {
            PointCount = points.Count,
            LineCount = lines.Count,
            PolygonCount = polygons.Count,
            PoiCount = pois.Count,
            TotalCount = points.Count + lines.Count + polygons.Count + pois.Count,
            Items = [.. points, .. lines, .. polygons, .. pois]
        };
    }

    private static List<AnalysisItemDto> ToItems(IReadOnlyList<FeatureDto> features, string type) =>
        features
            .Select(f => new AnalysisItemDto { Type = type, Id = f.Id, Name = f.Name })
            .ToList();
}
