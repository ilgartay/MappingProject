using System.Security.Claims;
using MapProject.Business.Dtos;
using MapProject.Business.Exceptions;
using MapProject.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MapProject.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _userService.GetAllAsync());
    }

    /// <summary>
    /// Kullanıcının durum kolonlarını günceller.
    /// PATCH kullanıyoruz: tüm nesneyi değil sadece değişen alanları gönderiyoruz.
    /// </summary>
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, UserStatusUpdateDto dto)
    {
        try
        {
            var updated = await _userService.UpdateStatusAsync(id, dto, GetCurrentUserId());

            return updated is null
                ? NotFound(new { message = "Kullanıcı bulunamadı." })
                : Ok(updated);
        }
        catch (InvalidUserOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Token'daki "sub" claim'i kullanıcı id'sini taşıyor (AuthService koyuyor).
    /// Claim okumak API katmanının işi; servise sade bir int gidiyor.
    /// </summary>
    private int GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub");

        return int.TryParse(raw, out var id) ? id : 0;
    }
}
