using MapProject.Business.Analysis;
using MapProject.Business.Dtos;

namespace MapProject.Business.GeoServer;

/// <summary>
/// GeoServer'ın WMS servisinden hazır resim ister.
///
/// WFS'ten farkı: WFS veriyi getirir, istemci çizer; WMS sunucuda çizip
/// PNG döner. Genel gösterim ve ısı haritası için WMS kullanıyoruz -
/// çizim/düzenleme için ise WFS, çünkü resmin üstündeki bir şekli
/// tıklayıp düzenlemek mümkün değil.
/// </summary>
public interface IGeoServerMapRenderer
{
    Task<MapImage> RenderAsync(MapRenderDto request, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Konum analizinin ağırlıklı ısı haritası.
    ///
    /// Kriterler GeoServer'a "viewparams" olarak gidiyor: parametreli SQL
    /// View her POI'ye kendi kategorisinin puanını yazıyor, ısı haritası
    /// SLD'si de noktaları o puana göre ağırlıklandırıyor.
    /// </summary>
    /// <param name="criteria">Doğrulanmış kriterler (2-5 adet, toplam 100).</param>
    /// <param name="areaWkt">Analiz alanı (POLYGON/MULTIPOLYGON, EPSG:4326).</param>
    Task<MapImage> RenderLocationAnalysisAsync(
        MapRenderDto request,
        IReadOnlyList<LocationCriterion> criteria,
        string areaWkt,
        CancellationToken cancellationToken = default);
}
