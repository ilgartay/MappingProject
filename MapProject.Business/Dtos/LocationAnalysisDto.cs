
namespace MapProject.Business.Dtos;

/// <summary>İl listesi girdisi; analiz ekranındaki açılır kutu bunu kullanıyor.</summary>
public class ProvinceDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Sınır (WKT, EPSG:4326). Listede boş gelir; tek il istendiğinde
    /// dolu döner - 81 ilin sınırını her liste isteğinde göndermenin
    /// anlamı yok, harita yalnızca seçileni çiziyor.
    /// </summary>
    public string? Wkt { get; set; }
}

/// <summary>
/// Konum analizi isteği. MapRenderDto'dan türüyor çünkü sonuç bir WMS
/// görüntüsü: bbox, genişlik ve yükseklik oradan geliyor.
/// </summary>
public class LocationAnalysisDto : MapRenderDto
{
    /// <summary>
    /// Kriterler "kategoriId:puan" çiftleri, virgülle ayrılmış.
    /// Örn. "4:70,5:30" - Restoran 70 puan, Kafe 30 puan.
    ///
    /// Metin olarak taşınıyor çünkü istek bir WMS görüntü isteği;
    /// OpenLayers parametreleri düz sorgu dizesi olarak gönderiyor.
    /// </summary>
    public string Criteria { get; set; } = string.Empty;

    /// <summary>Hedef bölge: il seçildiyse dolu.</summary>
    public int? ProvinceId { get; set; }

    /// <summary>Hedef bölge: haritaya poligon çizildiyse dolu (WKT, EPSG:4326).</summary>
    public string? AreaWkt { get; set; }
}
