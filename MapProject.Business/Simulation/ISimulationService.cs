namespace MapProject.Business.Simulation;

/// <summary>
/// Güzergah üzerinde araç simülasyonunu yürütür.
///
/// Tekil (singleton): çalışan simülasyonlar istekten isteğe yaşamalı.
/// Scoped olsaydı operatör "başlat" isteğini bitirdiği anda simülasyon
/// da ölürdü.
/// </summary>
public interface ISimulationService
{
    /// <summary>
    /// Güzergahın rotası üzerinde aracı ilk duraktan son durağa
    /// yürütmeye başlar. Zaten çalışıyorsa baştan başlatır.
    /// </summary>
    Task<VehicleState> StartAsync(int routeId, CancellationToken cancellationToken = default);

    /// <summary>Çalışan simülasyonu durdurur. Çalışmıyorsa false.</summary>
    bool Stop(int routeId);

    /// <summary>O anda çalışan simülasyonların son durumu.</summary>
    IReadOnlyList<VehicleState> GetActive();
}
