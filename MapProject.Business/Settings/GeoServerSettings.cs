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
    /// Isı haritası SLD'sinin adı. Yoğunluk hesabı bu stilin içindeki
    /// Heatmap dönüşümünde yapılıyor, kodumuzda değil.
    /// </summary>
    public string HeatmapStyle { get; set; } = "mapproject_heatmap";
}
