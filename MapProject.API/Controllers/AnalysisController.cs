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
public class AnalysisController : ApiControllerBase
{
    private readonly IAnalysisService _analysisService;

    public AnalysisController(IAnalysisService analysisService, ILogger<AnalysisController> logger)
        : base(logger)
    {
        _analysisService = analysisService;
    }

    /// <summary>
    /// Gönderilen poligonla kesişen envanterleri sayar.
    /// POST kullanıyoruz çünkü WKT metni uzun; URL'e sığmayabilir.
    /// Veritabanına hiçbir şey yazmıyor.
    /// </summary>
    [HttpPost("intersect")]
    [RequirePermission("analysis.run")]
    public async Task<IActionResult> Intersect(AnalysisRequestDto request)
    {
        try
        {
            return Ok(await _analysisService.IntersectAsync(request, User.GetUserId()));
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }
}
