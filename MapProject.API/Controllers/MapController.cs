using MapProject.API.Authorization;
using MapProject.API.Extensions;
using MapProject.Business.Dtos;
using MapProject.Business.GeoServer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MapProject.API.Controllers;

/// <summary>
/// GeoServer'ın WMS servisinden gelen harita görüntüleri.
///
/// Tarayıcı GeoServer'a doğrudan gitmiyor, buradan geçiyor. Sebebi
/// güvenlik: GeoServer katmanları kimlik sormuyor, dolayısıyla istemci
/// adresi kendisi kursa cql_filter'ı değiştirip başkasının çizimlerini
/// isteyebilirdi. Filtreyi token'daki kullanıcıya göre burada koyuyoruz.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MapController : ApiControllerBase
{
    // DİKKAT: Aşağıdaki parametrelerin adı "viewport", "request" değil.
    //
    // OpenLayers her WMS isteğine REQUEST=GetMap ekliyor. Parametreyi
    // "request" diye adlandırırsak ASP.NET bunu bir önek sanıp değerleri
    // "request.Bbox", "request.Width" adlarıyla arıyor; hiçbirini
    // bulamayınca tüm alanlar varsayılan kalıyor ve istek sessizce
    // 400'e düşüyor. Ad çakışması olmayan bir kelime seçmek gerekiyor.
    private readonly IGeoServerMapRenderer _renderer;

    public MapController(IGeoServerMapRenderer renderer, ILogger<MapController> logger)
        : base(logger)
    {
        _renderer = renderer;
    }

    /// <summary>
    /// Çizimlerin genel gösterimi. Sunucuda çizilmiş PNG döner.
    /// Yetki istemiyoruz: haritayı görüntülemek zaten herkese açık,
    /// GET /api/Feature de öyle.
    /// </summary>
    [HttpGet("features")]
    public Task<IActionResult> Features([FromQuery] MapRenderDto viewport)
    {
        viewport.LayerSet = MapLayerSet.Features;
        return RenderAsync(viewport);
    }

    /// <summary>
    /// Noktaların konum yoğunluğundan üretilen ısı haritası.
    /// Bir analiz aracı olduğu için ayrı yetkiye bağlı.
    /// </summary>
    [HttpGet("heatmap")]
    [RequirePermission("analysis.heatmap")]
    public Task<IActionResult> Heatmap([FromQuery] MapRenderDto viewport)
    {
        viewport.LayerSet = MapLayerSet.Heatmap;
        return RenderAsync(viewport);
    }

    private async Task<IActionResult> RenderAsync(MapRenderDto viewport)
    {
        try
        {
            var image = await _renderer.RenderAsync(viewport, User.GetUserId(), HttpContext.RequestAborted);
            return File(image.Content, image.ContentType);
        }
        catch (Exception ex)
        {
            return HandleError(ex);
        }
    }
}
