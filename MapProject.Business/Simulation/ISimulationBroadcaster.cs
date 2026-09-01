namespace MapProject.Business.Simulation;

/// <summary>
/// Araç konumunu takipçilere ulaştıran taraf.
///
/// Business katmanı SignalR'ı tanımıyor: SignalR bir ASP.NET Core
/// parçası ve buraya bağımlılık eklemek katmanları ters çevirirdi.
/// Business "şu güzergahı izleyenlere şunu duyur" diyor; bunu neyin
/// yaptığı (SignalR, WebSocket, log) API katmanının kararı.
/// </summary>
public interface ISimulationBroadcaster
{
    Task PublishAsync(VehicleState state, CancellationToken cancellationToken = default);
}
