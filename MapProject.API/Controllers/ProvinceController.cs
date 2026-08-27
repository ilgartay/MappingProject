using MapProject.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MapProject.API.Controllers;

/// <summary>
/// İl sınırları. Yetki istemiyor: konum analizi Kullanıcı rolüne de açık,
/// hedef bölge seçebilmesi için listeyi görmesi gerekiyor.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProvinceController : ApiControllerBase
{
    private readonly IProvinceService _provinceService;

    public ProvinceController(IProvinceService provinceService, ILogger<ProvinceController> logger)
        : base(logger)
    {
        _provinceService = provinceService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            return Ok(await _provinceService.GetAllAsync());
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>Tek il, sınırıyla birlikte; harita seçileni çizebilsin diye.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var province = await _provinceService.GetByIdAsync(id);
            return province is null ? NotFound() : Ok(province);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }
}
