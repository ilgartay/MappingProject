using MapProject.API.Authorization;
using MapProject.API.Extensions;
using MapProject.Business.Dtos;
using MapProject.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MapProject.API.Controllers;

/// <summary>
/// İlgi noktaları (POI).
///
/// Listeleme kullanıcıya göre filtrelenmiyor: bir eczanenin konumu
/// herkes için aynı bilgi. Ekleyen kullanıcı yalnızca bilgi olarak
/// taşınıyor, admin panelinde gösteriliyor.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PoiController : ApiControllerBase
{
    private readonly IPoiService _poiService;

    public PoiController(IPoiService poiService, ILogger<PoiController> logger)
        : base(logger)
    {
        _poiService = poiService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            return Ok(await _poiService.GetAllAsync());
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var poi = await _poiService.GetByIdAsync(id);
            return poi is null ? NotFound() : Ok(poi);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPost]
    [RequirePermission("poi.create")]
    public async Task<IActionResult> Create(PoiSaveDto dto)
    {
        try
        {
            var created = await _poiService.CreateAsync(dto, User.GetUserId());
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Güncelleme POI yönetimi yetkisi istiyor: POI'ler paylaşılan veri,
    /// operatörün başkasının eklediğini değiştirmesi doğru olmaz.
    /// </summary>
    [HttpPut("{id:int}")]
    [RequirePermission("poi.manage")]
    public async Task<IActionResult> Update(int id, PoiSaveDto dto)
    {
        try
        {
            var updated = await _poiService.UpdateAsync(id, dto, User.GetUserId());
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpDelete("{id:int}")]
    [RequirePermission("poi.manage")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            return await _poiService.DeleteAsync(id) ? NoContent() : NotFound();
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }
}
