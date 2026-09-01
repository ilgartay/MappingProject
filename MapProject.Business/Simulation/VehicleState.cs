namespace MapProject.Business.Simulation;

/// <summary>
/// Simülasyondaki aracın o anki durumu. SignalR ile yayınlanan ve
/// REST'ten okunan tek model bu: istemci iki kaynaktan aynı biçimi
/// alsın, "canlı gelen veri" ile "sayfa açılışında gelen veri" ayrı
/// şekillerde işlenmesin.
/// </summary>
public class VehicleState
{
    public int RouteId { get; set; }
    public string RouteName { get; set; } = string.Empty;

    /// <summary>Hattın rengi; araç ikonu da bu renkte çiziliyor.</summary>
    public string RouteColor { get; set; } = string.Empty;

    public double Longitude { get; set; }
    public double Latitude { get; set; }

    /// <summary>Tamamlanan yüzde, 0-100.</summary>
    public double Progress { get; set; }

    /// <summary>Alınan yol ve toplam yol, metre.</summary>
    public double TravelledMetres { get; set; }
    public double TotalMetres { get; set; }

    /// <summary>Aracın gittiği yön, kuzeyden saat yönünde derece. İkon buna göre dönüyor.</summary>
    public double Heading { get; set; }

    public DateTime StartedAt { get; set; }

    /// <summary>Son yayın: araç son durağa vardı, simülasyon bitti.</summary>
    public bool IsFinished { get; set; }
}
