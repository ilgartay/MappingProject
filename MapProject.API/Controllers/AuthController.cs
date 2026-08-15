using MapProject.Business.Dtos;
using MapProject.Business.Services;
using Microsoft.AspNetCore.Mvc;

namespace MapProject.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
        : base(logger)
    {
        _authService = authService;
    }

    /// <summary>Kullanıcı adı + şifre ile giriş yapar, başarılıysa JWT döner.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequestDto request)
    {
        try
        {
            var result = await _authService.LoginAsync(request);

            if (result is null)
            {
                return Unauthorized(new { message = "Kullanıcı adı veya şifre hatalı." });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }
}
