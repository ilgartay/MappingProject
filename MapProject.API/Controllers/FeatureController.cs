using MapProject.Business.Dtos;
using MapProject.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MapProject.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FeatureController : ControllerBase
{
    private readonly IFeatureService _featureService;

    public FeatureController(IFeatureService featureService)
    {
        _featureService = featureService;
    }

    /// <summary>Üç tablodaki tüm geometrileri WKT olarak döner.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _featureService.GetAllAsync());
    }

    [HttpPost("point")]
    public Task<IActionResult> CreatePoint(FeatureCreateDto dto) =>
        CreateAsync(() => _featureService.CreatePointAsync(dto));

    [HttpPost("line")]
    public Task<IActionResult> CreateLine(FeatureCreateDto dto) =>
        CreateAsync(() => _featureService.CreateLineAsync(dto));

    [HttpPost("polygon")]
    public Task<IActionResult> CreatePolygon(FeatureCreateDto dto) =>
        CreateAsync(() => _featureService.CreatePolygonAsync(dto));

    [HttpDelete("point/{id:int}")]
    public Task<IActionResult> DeletePoint(int id) =>
        DeleteAsync(() => _featureService.DeletePointAsync(id));

    [HttpDelete("line/{id:int}")]
    public Task<IActionResult> DeleteLine(int id) =>
        DeleteAsync(() => _featureService.DeleteLineAsync(id));

    [HttpDelete("polygon/{id:int}")]
    public Task<IActionResult> DeletePolygon(int id) =>
        DeleteAsync(() => _featureService.DeletePolygonAsync(id));

    /// <summary>
    /// Silinen kayıt için gövde döndürecek bir şey yok: 204 No Content.
    /// Kayıt zaten yoksa 404.
    /// </summary>
    private static async Task<IActionResult> DeleteAsync(Func<Task<bool>> delete)
    {
        return await delete()
            ? new NoContentResult()
            : new NotFoundObjectResult(new { message = "Kayıt bulunamadı." });
    }

    /// <summary>
    /// Üç POST action'ı da aynı hata işleyişini paylaşıyor:
    /// geometri hatası kullanıcı hatasıdır, 500 değil 400 dönmeli.
    /// </summary>
    private async Task<IActionResult> CreateAsync(Func<Task<FeatureDto>> create)
    {
        try
        {
            var created = await create();
            return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
        }
        catch (InvalidGeometryException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
