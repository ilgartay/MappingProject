namespace MapProject.Business.Settings;

/// <summary>
/// Docker'da çalışan OSRM sunucusunun ayarları.
///
/// Adres appsettings'te: sunucuyu başka bir makineye taşımak ya da
/// geçici olarak public OSRM'e bakmak için kod değiştirmek gerekmesin.
/// </summary>
public class OsrmSettings
{
    public const string SectionName = "Osrm";

    /// <summary>
    /// Kök adres. 5000 macOS'ta AirPlay Receiver tarafından tutulduğu
    /// için konteyner 5001'e bağlanıyor.
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:5001";

    /// <summary>
    /// Yol profili. Konteyner car.lua ile ön işlendiği için "driving";
    /// yaya ya da bisiklet istenirse veri yeniden işlenmeli.
    /// </summary>
    public string Profile { get; set; } = "driving";
}
