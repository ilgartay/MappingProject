using MapProject.Business.Dtos;

namespace MapProject.Business.Services;

/// <summary>
/// Ulaşım modülü: güzergahlar ve onlara bağlı duraklar.
///
/// Okuma herkese açık (Ulaşım Kullanıcısı rolü de görebilmeli);
/// değiştirme route.manage / stop.manage yetkisi istiyor.
/// </summary>
public interface ITransportService
{
    /// <summary>Güzergahlar, durakları sıralı halde.</summary>
    Task<IReadOnlyList<RouteDto>> GetRoutesAsync();

    Task<RouteDto?> GetRouteAsync(int id);

    Task<RouteDto> CreateRouteAsync(RouteSaveDto dto);

    /// <summary>Güzergah yoksa null.</summary>
    Task<RouteDto?> UpdateRouteAsync(int id, RouteSaveDto dto);

    /// <summary>Soft delete. Durağı varsa hata verir.</summary>
    Task<bool> DeleteRouteAsync(int id);

    /// <summary>Durak güzergahın sonuna eklenir.</summary>
    Task<StopDto> CreateStopAsync(StopSaveDto dto);

    Task<StopDto?> UpdateStopAsync(int id, StopSaveDto dto);

    Task<bool> DeleteStopAsync(int id);

    /// <summary>Sürükle-bırak sonrası sırayı kaydeder.</summary>
    Task<RouteDto?> ReorderStopsAsync(int routeId, StopOrderDto dto);
}
