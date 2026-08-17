using MapProject.API.Authorization;
using MapProject.API.Extensions;
using MapProject.Business.Dtos;
using MapProject.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MapProject.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ApiControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService, ILogger<UserController> logger)
        : base(logger)
    {
        _userService = userService;
    }

    /// <summary>Giriş yapan kullanıcının kendi bilgisi ve etkin yetkileri.</summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrent()
    {
        try
        {
            var current = await _userService.GetCurrentAsync(User.GetUserId());
            return current is null ? NotFoundResponse() : Ok(current);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpGet]
    [RequirePermission("user.manage")]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            return Ok(await _userService.GetAllAsync());
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPost]
    [RequirePermission("user.manage")]
    public async Task<IActionResult> Create(UserSaveDto dto)
    {
        try
        {
            var created = await _userService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPut("{id:int}")]
    [RequirePermission("user.manage")]
    public async Task<IActionResult> Update(int id, UserSaveDto dto)
    {
        try
        {
            var updated = await _userService.UpdateAsync(id, dto, User.GetUserId());
            return updated is null ? NotFoundResponse() : Ok(updated);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpDelete("{id:int}")]
    [RequirePermission("user.manage")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            return await _userService.DeleteAsync(id, User.GetUserId())
                ? NoContent()
                : NotFoundResponse();
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Kullanıcının durum kolonlarını günceller.
    /// PATCH kullanıyoruz: tüm nesneyi değil sadece değişen alanları gönderiyoruz.
    /// </summary>
    [HttpPatch("{id:int}/status")]
    [RequirePermission("user.manage")]
    public async Task<IActionResult> UpdateStatus(int id, UserStatusUpdateDto dto)
    {
        try
        {
            var updated = await _userService.UpdateStatusAsync(id, dto, User.GetUserId());
            return updated is null ? NotFoundResponse() : Ok(updated);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>Roller + her yetkinin doğrudan mı rolden mi geldiği.</summary>
    [HttpGet("{id:int}/access")]
    [RequirePermission("user.manage")]
    public async Task<IActionResult> GetAccess(int id)
    {
        try
        {
            var access = await _userService.GetAccessAsync(id);
            return access is null ? NotFoundResponse() : Ok(access);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPut("{id:int}/access")]
    [RequirePermission("user.manage")]
    public async Task<IActionResult> SaveAccess(int id, UserAccessSaveDto dto)
    {
        try
        {
            var access = await _userService.SaveAccessAsync(id, dto);
            return access is null ? NotFoundResponse() : Ok(access);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    private IActionResult NotFoundResponse() =>
        NotFound(new { message = "Kullanıcı bulunamadı." });
}
