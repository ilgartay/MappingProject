namespace MapProject.Business.Settings;

/// <summary>Araç simülasyonunun hız ve sıklık ayarları.</summary>
public class SimulationSettings
{
    public const string SectionName = "Simulation";

    /// <summary>
    /// Gerçek sürüş süresinin kaça bölüneceği. OSRM 17 dakika diyorsa
    /// 20'ye bölünce simülasyon ~51 saniye sürüyor: sunumda izlenebilir,
    /// ama araç yine de hattın gerçek uzunluğuyla orantılı hareket ediyor.
    /// </summary>
    public double SpeedFactor { get; set; } = 20;

    /// <summary>
    /// Simülasyon süresi bu aralığa kırpılıyor. Çok kısa hatlar göz
    /// açıp kapayana kadar bitmesin, çok uzunları da dakikalarca sürmesin.
    /// </summary>
    public int MinSeconds { get; set; } = 20;
    public int MaxSeconds { get; set; } = 180;

    /// <summary>
    /// İki yayın arası milisaniye. 400 ms akıcı görünüyor; daha sık
    /// yayın ağı meşgul ediyor, daha seyreği aracı zıplatıyor.
    /// </summary>
    public int TickMilliseconds { get; set; } = 400;
}
