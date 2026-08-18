using MapProject.API.Authorization;
using MapProject.API.Extensions;
using MapProject.Business.Dtos;
using MapProject.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MapProject.API.Controllers;

/// <summary>
/// Kullanıcı ve rollere çizim alanı tanımlar.
/// Alan tanımlıysa o kullanıcı alanın dışına çizim yapamaz.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[RequirePermission("geo.manage")]
public class GeoPermissionController : ApiControllerBase
{
    private readonly IGeoPermissionService _geoPermissionService;

    public GeoPermissionController(
        IGeoPermissionService geoPermissionService,
        ILogger<GeoPermissionController> logger)
        : base(logger)
    {
        _geoPermissionService = geoPermissionService;
    }

    [HttpGet("user/{userId:int}")]
    public async Task<IActionResult> GetForUser(int userId)
    {
        try
        {
            // Tanım yoksa 404 değil, boş cevap: "alan yok" normal bir durum.
            return Ok(await _geoPermissionService.GetForUserAsync(userId));
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpGet("role/{roleId:int}")]
    public async Task<IActionResult> GetForRole(int roleId)
    {
        try
        {
            return Ok(await _geoPermissionService.GetForRoleAsync(roleId));
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPut("user/{userId:int}")]
    public async Task<IActionResult> SaveForUser(int userId, GeoPermissionSaveDto dto)
    {
        try
        {
            var saved = await _geoPermissionService.SaveForUserAsync(userId, dto, User.GetUserId());
            return Ok(saved);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPut("role/{roleId:int}")]
    public async Task<IActionResult> SaveForRole(int roleId, GeoPermissionSaveDto dto)
    {
        try
        {
            var saved = await _geoPermissionService.SaveForRoleAsync(roleId, dto, User.GetUserId());
            return Ok(saved);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }
}
