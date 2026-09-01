using MapProject.Business.Simulation;
using Microsoft.AspNetCore.SignalR;

namespace MapProject.API.Hubs;

/// <summary>
/// Business katmanının ISimulationBroadcaster sözleşmesini SignalR ile
/// karşılar. Business "duyur" diyor, taşıma kararı burada.
/// </summary>
public class SignalRSimulationBroadcaster : ISimulationBroadcaster
{
    private readonly IHubContext<SimulationHub> _hub;

    public SignalRSimulationBroadcaster(IHubContext<SimulationHub> hub)
    {
        _hub = hub;
    }

    public Task PublishAsync(VehicleState state, CancellationToken cancellationToken = default) =>
        _hub.Clients
            .Group(SimulationHub.GroupName(state.RouteId))
            .SendAsync("VehicleMoved", state, cancellationToken);
}
