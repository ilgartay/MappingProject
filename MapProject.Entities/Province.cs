using NetTopologySuite.Geometries;

namespace MapProject.Entities;

/// <summary>
/// il - konum analizinde hedef bölge seçmek için il sınırları.
///
/// Sınırlar YAKLAŞIKTIR: gerçek il sınır verisi elimizde olmadığı için
/// 81 il merkezinden Voronoi (Thiessen) hücreleri üretilip Türkiye
/// sınırıyla kırpıldı. Her nokta kendisine en yakın alanı alıyor, bu da
/// gerçeğe yakın bir bölünme veriyor - ama küçük iller olduğundan geniş,
/// büyük iller olduğundan dar çıkıyor.
///
/// Analiz alanı seçimi için yeterli; idari işlem için değil.
/// </summary>
public class Province
{
    public int Id { get; set; }

    /// <summary>ad</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>EPSG:4326. Voronoi kırpması sonucu çok parçalı olabiliyor.</summary>
    public required Geometry Geometry { get; set; }
}
