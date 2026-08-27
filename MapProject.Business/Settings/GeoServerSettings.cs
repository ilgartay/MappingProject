namespace MapProject.Business.Settings;

/// <summary>
/// appsettings.json içindeki "GeoServer" bölümünün karşılığı.
/// Katman adları da buradan geliyor: GeoServer'da katmanı yeniden
/// adlandırmak gerekirse kod değil ayar dosyası değişsin.
/// </summary>
public class GeoServerSettings
{
    public const string SectionName = "GeoServer";

    /// <summary>Sunucunun kökü. Sonundaki eğik çizgi olmadan.</summary>
    public string BaseUrl { get; set; } = "http://localhost:8080/geoserver";

    /// <summary>Katmanları barındıran çalışma alanı.</summary>
    public string Workspace { get; set; } = "mapproject";

    /// <summary>
    /// WFS okuma isteği için kullanıcı. Katmanlar herkese açık olsa
    /// kimlik gerekmezdi; yine de gönderiyoruz ki katmanları yetkiye
    /// kapattığımızda tek satır bile değişmesin.
    /// </summary>
    public string Username { get; set; } = "admin";

    /// <summary>Şifre appsettings.json'a yazılmıyor; user-secrets ya da ortam değişkeni.</summary>
    public string Password { get; set; } = string.Empty;

    // Katmanlar tabloların kendisi değil, GeoServer'daki SQL View'lar.
    // View "WHERE is_deleted = false" içerdiği için silinmiş kayıtlar
    // GeoServer'dan hiç çıkmıyor; is_deleted kolonu da dışarı verilmiyor.
    public string PointLayer { get; set; } = "vw_point";
    public string LineLayer { get; set; } = "vw_line";
    public string PolygonLayer { get; set; } = "vw_polygon";

    /// <summary>
    /// POI katmanı. Bu view kategori ve kullanıcı tablolarını da join'liyor,
    /// böylece liste tek istekte kategori adı ve ekleyen kullanıcıyla geliyor.
    /// </summary>
    public string PoiLayer { get; set; } = "vw_poi";

    /// <summary>
    /// POI kategorilerinin stilleri. Her biri kendi kategorisini filtreleyip
    /// kendi ikonuyla çiziyor; WMS aynı katmanı bu stillerin her biri için
    /// bir kez çizdiği için hepsi tek resimde birleşiyor.
    ///
    /// Sıra önemli: sondaki stil en üstte çiziliyor. "poi_diger" listede
    /// son değil çünkü yedek stil diğerlerinin üstünü kapatmasın.
    ///
    /// Yeni bir kategoriye kendi görünümü verilecekse SLD'si GeoServer'a
    /// yüklenip adı buraya eklenmeli; eklenmezse "poi_diger" devreye girer.
    /// </summary>
    public IReadOnlyList<string> PoiStyles { get; set; } =
        ["poi_diger", "poi_konaklama", "poi_yeme_icme", "poi_saglik"];

    /// <summary>
    /// Konum analizinin katmanı: parametreli SQL View. Kriterlerin puanı
    /// buraya viewparams olarak geçiyor ve "agirlik" kolonuna yazılıyor.
    /// </summary>
    public string WeightedPoiLayer { get; set; } = "vw_poi_agirlikli";

    /// <summary>Ağırlığı dikkate alan ısı haritası stili.</summary>
    public string WeightedHeatmapStyle { get; set; } = "poi_isi_agirlikli";

    /// <summary>
    /// Isı haritası SLD'sinin adı. Yoğunluk hesabı bu stilin içindeki
    /// Heatmap dönüşümünde yapılıyor, kodumuzda değil.
    /// </summary>
    public string HeatmapStyle { get; set; } = "mapproject_heatmap";
}
