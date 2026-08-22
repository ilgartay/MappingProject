using MapProject.API.Authorization;
using MapProject.Business.Dtos;
using MapProject.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MapProject.API.Controllers;

/// <summary>
/// POI kategorileri. Okuma herkese açık - operatörün açılır kutusu da
/// buradan besleniyor; değiştirme yalnızca kategori yetkisi olana.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PoiCategoryController : ApiControllerBase
{
    private readonly IPoiCategoryService _categoryService;

    public PoiCategoryController(IPoiCategoryService categoryService, ILogger<PoiCategoryController> logger)
        : base(logger)
    {
        _categoryService = categoryService;
    }

    /// <summary>Ağaç yapısı; admin panelindeki kategori listesi bunu kullanıyor.</summary>
    [HttpGet("tree")]
    public async Task<IActionResult> GetTree()
    {
        try
        {
            return Ok(await _categoryService.GetTreeAsync());
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    /// <summary>Düz liste; POI formundaki açılır kutu bunu kullanıyor.</summary>
    [HttpGet]
    public async Task<IActionResult> GetFlat()
    {
        try
        {
            return Ok(await _categoryService.GetFlatAsync());
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPost]
    [RequirePermission("category.manage")]
    public async Task<IActionResult> Create(PoiCategorySaveDto dto)
    {
        try
        {
            return Ok(await _categoryService.CreateAsync(dto));
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpPut("{id:int}")]
    [RequirePermission("category.manage")]
    public async Task<IActionResult> Update(int id, PoiCategorySaveDto dto)
    {
        try
        {
            var updated = await _categoryService.UpdateAsync(id, dto);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }

    [HttpDelete("{id:int}")]
    [RequirePermission("category.manage")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            return await _categoryService.DeleteAsync(id) ? NoContent() : NotFound();
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }
}
