using MapProject.Business.Dtos;
using MapProject.Business.Exceptions;
using MapProject.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MapProject.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnalysisController : ControllerBase
{
    private readonly IAnalysisService _analysisService;

    public AnalysisController(IAnalysisService analysisService)
    {
        _analysisService = analysisService;
    }

    /// <summary>
    /// Gönderilen poligonla kesişen envanterleri sayar.
    /// POST kullanıyoruz çünkü WKT metni uzun; URL'e sığmayabilir.
    /// Veritabanına hiçbir şey yazmıyor.
    /// </summary>
    [HttpPost("intersect")]
    public async Task<IActionResult> Intersect(AnalysisRequestDto request)
    {
        try
        {
            return Ok(await _analysisService.IntersectAsync(request));
        }
        catch (InvalidGeometryException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
