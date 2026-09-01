using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MapProject.API.Hubs;

/// <summary>
/// Araç konumlarının canlı yayınlandığı SignalR merkezi.
///
/// Neden gruplar: her araç konumu her bağlı istemciye gitseydi, on
/// güzergahın çalıştığı bir sistemde herkes izlemediği dokuz aracın
/// verisini de alırdı. İstemci hangi hattı takip ediyorsa yalnızca o
/// grubun yayınını alıyor.
///
/// [Authorize] burada da geçerli: hub bir HTTP ucu kadar açık bir kapı.
/// Token WebSocket'te başlıkla gönderilemediği için sorgu dizesinden
/// okunuyor; ayarı Program.cs'te.
/// </summary>
[Authorize]
public class SimulationHub : Hub
{
    /// <summary>Güzergah id'sinden grup adı. Tek yerde üretiliyor ki yayıncıyla burası ayrışmasın.</summary>
    public static string GroupName(int routeId) => $"route-{routeId}";

    /// <summary>"Takip Et": istemci bu güzergahın yayınına katılır.</summary>
    public Task JoinRoute(int routeId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, GroupName(routeId));

    /// <summary>"Takibi Bırak".</summary>
    public Task LeaveRoute(int routeId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(routeId));

    // Bağlantı koptuğunda gruplardan çıkarmaya gerek yok: SignalR
    // bağlantıyı gruplardan kendisi düşürüyor.
}
