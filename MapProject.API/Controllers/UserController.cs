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

    [HttpGet]
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

    /// <summary>
    /// Kullanıcının durum kolonlarını günceller.
    /// PATCH kullanıyoruz: tüm nesneyi değil sadece değişen alanları gönderiyoruz.
    /// </summary>
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, UserStatusUpdateDto dto)
    {
        try
        {
            var updated = await _userService.UpdateStatusAsync(id, dto, User.GetUserId());

            return updated is null
                ? NotFound(new { message = "Kullanıcı bulunamadı." })
                : Ok(updated);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }
}
