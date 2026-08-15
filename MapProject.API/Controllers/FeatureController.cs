using MapProject.API.Extensions;
using MapProject.Business.Dtos;
using MapProject.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MapProject.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Geçerli JWT olmadan bu controller'ın hiçbir action'ına erişilemez.
public class FeatureController : ApiControllerBase
{
    private readonly IFeatureService _featureService;

    public FeatureController(IFeatureService featureService, ILogger<FeatureController> logger)
        : base(logger)
    {
        _featureService = featureService;
    }

    /// <summary>Giriş yapan kullanıcının çizimlerini WKT olarak döner.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            return Ok(await _featureService.GetAllAsync(User.GetUserId()));
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    // --- Oluşturma ---

    [HttpPost("point")]
    public async Task<IActionResult> CreatePoint(FeatureCreateDto dto)
    {
        try
        {
            var created = await _featureService.CreatePointAsync(dto, User.GetUserId());
            return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPost("line")]
    public async Task<IActionResult> CreateLine(FeatureCreateDto dto)
    {
        try
        {
            var created = await _featureService.CreateLineAsync(dto, User.GetUserId());
            return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPost("polygon")]
    public async Task<IActionResult> CreatePolygon(FeatureCreateDto dto)
    {
        try
        {
            var created = await _featureService.CreatePolygonAsync(dto, User.GetUserId());
            return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    // --- Güncelleme: isim, renk ve geometri ---

    [HttpPut("point/{id:int}")]
    public async Task<IActionResult> UpdatePoint(int id, FeatureUpdateDto dto)
    {
        try
        {
            var updated = await _featureService.UpdatePointAsync(id, dto, User.GetUserId());
            return updated is null ? NotFoundResponse() : Ok(updated);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPut("line/{id:int}")]
    public async Task<IActionResult> UpdateLine(int id, FeatureUpdateDto dto)
    {
        try
        {
            var updated = await _featureService.UpdateLineAsync(id, dto, User.GetUserId());
            return updated is null ? NotFoundResponse() : Ok(updated);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPut("polygon/{id:int}")]
    public async Task<IActionResult> UpdatePolygon(int id, FeatureUpdateDto dto)
    {
        try
        {
            var updated = await _featureService.UpdatePolygonAsync(id, dto, User.GetUserId());
            return updated is null ? NotFoundResponse() : Ok(updated);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    // --- Soft delete ---

    [HttpDelete("point/{id:int}")]
    public async Task<IActionResult> DeletePoint(int id)
    {
        try
        {
            return await _featureService.DeletePointAsync(id, User.GetUserId())
                ? NoContent()
                : NotFoundResponse();
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpDelete("line/{id:int}")]
    public async Task<IActionResult> DeleteLine(int id)
    {
        try
        {
            return await _featureService.DeleteLineAsync(id, User.GetUserId())
                ? NoContent()
                : NotFoundResponse();
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpDelete("polygon/{id:int}")]
    public async Task<IActionResult> DeletePolygon(int id)
    {
        try
        {
            return await _featureService.DeletePolygonAsync(id, User.GetUserId())
                ? NoContent()
                : NotFoundResponse();
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Kayıt yok, silinmiş ya da başkasına ait - üçünde de aynı cevap.
    /// "Var ama senin değil" demek başkasının verisi hakkında bilgi sızdırır.
    /// </summary>
    private IActionResult NotFoundResponse() =>
        NotFound(new { message = "Kayıt bulunamadı." });
}
