using MapProject.API.Authorization;
using MapProject.Business.Dtos;
using MapProject.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MapProject.API.Controllers;

/// <summary>
/// Ulaşım modülü: güzergahlar ve duraklar.
///
/// Okuma uçları yetki istemiyor - Ulaşım Kullanıcısı rolünün hattı ve
/// duraklarını görebilmesi gerekiyor. Değiştirme ise route.manage /
/// stop.manage istiyor; bu yetkiler yalnızca Ulaşım Operatörü ve
/// Admin rollerinde var.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransportController : ApiControllerBase
{
    private readonly ITransportService _transportService;

    public TransportController(ITransportService transportService, ILogger<TransportController> logger)
        : base(logger)
    {
        _transportService = transportService;
    }

    // --- Güzergah ---

    [HttpGet("routes")]
    public async Task<IActionResult> GetRoutes()
    {
        try
        {
            return Ok(await _transportService.GetRoutesAsync());
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpGet("routes/{id:int}")]
    public async Task<IActionResult> GetRoute(int id)
    {
        try
        {
            var route = await _transportService.GetRouteAsync(id);
            return route is null ? NotFound() : Ok(route);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPost("routes")]
    [RequirePermission("route.manage")]
    public async Task<IActionResult> CreateRoute(RouteSaveDto dto)
    {
        try
        {
            var created = await _transportService.CreateRouteAsync(dto);
            return CreatedAtAction(nameof(GetRoute), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPut("routes/{id:int}")]
    [RequirePermission("route.manage")]
    public async Task<IActionResult> UpdateRoute(int id, RouteSaveDto dto)
    {
        try
        {
            var updated = await _transportService.UpdateRouteAsync(id, dto);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpDelete("routes/{id:int}")]
    [RequirePermission("route.manage")]
    public async Task<IActionResult> DeleteRoute(int id)
    {
        try
        {
            return await _transportService.DeleteRouteAsync(id) ? NoContent() : NotFound();
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>Sürükle-bırak sonrası yeni durak sırası.</summary>
    [HttpPut("routes/{id:int}/order")]
    [RequirePermission("stop.manage")]
    public async Task<IActionResult> ReorderStops(int id, StopOrderDto dto)
    {
        try
        {
            var route = await _transportService.ReorderStopsAsync(id, dto);
            return route is null ? NotFound() : Ok(route);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    // --- Durak ---

    [HttpPost("stops")]
    [RequirePermission("stop.manage")]
    public async Task<IActionResult> CreateStop(StopSaveDto dto)
    {
        try
        {
            return Ok(await _transportService.CreateStopAsync(dto));
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPut("stops/{id:int}")]
    [RequirePermission("stop.manage")]
    public async Task<IActionResult> UpdateStop(int id, StopSaveDto dto)
    {
        try
        {
            var updated = await _transportService.UpdateStopAsync(id, dto);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpDelete("stops/{id:int}")]
    [RequirePermission("stop.manage")]
    public async Task<IActionResult> DeleteStop(int id)
    {
        try
        {
            return await _transportService.DeleteStopAsync(id) ? NoContent() : NotFound();
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }
}
