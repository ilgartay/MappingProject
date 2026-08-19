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

    // GeoServer'daki katman adları = PostGIS tablo adları.
    public string PointLayer { get; set; } = "tbl_point";
    public string LineLayer { get; set; } = "tbl_line";
    public string PolygonLayer { get; set; } = "tbl_polygon";
}
