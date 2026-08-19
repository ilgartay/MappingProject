using MapProject.Business.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace MapProject.API.Controllers;

/// <summary>
/// Tüm controller'ların ortak hata çevirisi.
/// Her uç kendi try-catch'ini yazıyor ama hatayı HTTP durumuna çevirme
/// kuralı tek yerde: aynı hata her yerde aynı cevabı üretiyor.
/// </summary>
public abstract class ApiControllerBase : ControllerBase
{
    private readonly ILogger _logger;

    protected ApiControllerBase(ILogger logger)
    {
        _logger = logger;
    }

    protected IActionResult HandleError(Exception exception)
    {
        switch (exception)
        {
            // Kullanıcı hatası: gönderilen veri kurallara uymuyor.
            case InvalidGeometryException:
            case InvalidUserOperationException:
                return BadRequest(new { message = exception.Message });

            // Dış servis hatası: bizim kodumuz sağlam, GeoServer cevap vermiyor.
            // 503 "şu an kullanılamıyor" demek; 500 gibi kalıcı bir arıza
            // izlenimi vermediği için istemci tekrar denemeyi seçebilir.
            case GeoServerException:
                _logger.LogError(exception, "GeoServer hatası: {Path}", HttpContext.Request.Path);

                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new { message = "Harita servisine (GeoServer) şu an ulaşılamıyor." });

            default:
                // Beklenmeyen hata. Mesajı istemciye vermiyoruz - iç detay
                // (bağlantı dizesi, dosya yolu, SQL) sızdırabilir. Sunucuya loglayıp
                // kullanıcıya sade bir metin dönüyoruz.
                _logger.LogError(exception, "Beklenmeyen hata: {Path}", HttpContext.Request.Path);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { message = "Beklenmeyen bir hata oluştu." });
        }
    }
}
