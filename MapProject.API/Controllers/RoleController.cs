using MapProject.API.Authorization;
using MapProject.Business.Dtos;
using MapProject.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MapProject.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
// Rol ekranının tamamı tek yetkiye bağlı.
[RequirePermission("role.manage")]
public class RoleController : ApiControllerBase
{
    private readonly IRoleService _roleService;

    public RoleController(IRoleService roleService, ILogger<RoleController> logger)
        : base(logger)
    {
        _roleService = roleService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            return Ok(await _roleService.GetAllAsync());
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>Atama ekranlarını dolduran yetki listesi.</summary>
    [HttpGet("permissions")]
    public async Task<IActionResult> GetPermissions()
    {
        try
        {
            return Ok(await _roleService.GetPermissionsAsync());
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(RoleSaveDto dto)
    {
        try
        {
            var created = await _roleService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, RoleSaveDto dto)
    {
        try
        {
            var updated = await _roleService.UpdateAsync(id, dto);
            return updated is null ? NotFound(new { message = "Rol bulunamadı." }) : Ok(updated);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            return await _roleService.DeleteAsync(id)
                ? NoContent()
                : NotFound(new { message = "Rol bulunamadı." });
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }
}
