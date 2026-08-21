using System.Globalization;
using System.Net;
using MapProject.Business.Dtos;
using MapProject.Business.Exceptions;
using MapProject.Business.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MapProject.Business.GeoServer;

public class GeoServerMapRenderer : IGeoServerMapRenderer
{
    /// <summary>
    /// Tek bir istekte üretilecek en büyük resim. Sınır olmasaydı istemci
    /// 20000x20000 isteyip sunucunun belleğini tüketebilirdi.
    /// </summary>
    private const int MaxDimension = 4096;

    private readonly HttpClient _http;
    private readonly GeoServerSettings _settings;
    private readonly ILogger<GeoServerMapRenderer> _logger;

    public GeoServerMapRenderer(
        HttpClient http,
        IOptions<GeoServerSettings> settings,
        ILogger<GeoServerMapRenderer> logger)
    {
        _http = http;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<MapImage> RenderAsync(
        MapRenderDto request, int userId, CancellationToken cancellationToken = default)
    {
        Validate(request);

        var url = BuildGetMapUrl(request, userId);

        HttpResponseMessage response;

        try
        {
            response = await _http.GetAsync(url, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new GeoServerException("GeoServer'a ulaşılamadı (WMS).", ex);
        }

        var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;

        // GeoServer WMS hatalarını 200 ile ve XML gövdesiyle dönebiliyor.
        // Sadece durum koduna bakarsak bozuk bir XML'i PNG diye istemciye
        // gönderir, tarayıcı da sessizce boş resim gösterirdi.
        if (!response.IsSuccessStatusCode || !contentType.StartsWith("image/", StringComparison.Ordinal))
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogError(
                "GeoServer WMS beklenmeyen cevap. Durum: {Status}, tip: {ContentType}, gövde: {Body}",
                (int)response.StatusCode, contentType, Truncate(body, 500));

            throw new GeoServerException(
                response.StatusCode == HttpStatusCode.Unauthorized
                    ? "GeoServer kimlik bilgileri reddedildi."
                    : "Harita görüntüsü üretilemedi.");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        return new MapImage(bytes, contentType);
    }

    private static void Validate(MapRenderDto request)
    {
        if (request.Width <= 0 || request.Height <= 0 ||
            request.Width > MaxDimension || request.Height > MaxDimension)
        {
            throw new InvalidUserOperationException(
                $"Genişlik ve yükseklik 1 ile {MaxDimension} arasında olmalı.");
        }

        // BBOX doğrudan GeoServer'a gidiyor; biçimini burada doğruluyoruz ki
        // istemci araya başka parametre sıkıştıramasın.
        var parts = request.Bbox.Split(',');

        if (parts.Length != 4 ||
            !parts.All(p => double.TryParse(p, NumberStyles.Float, CultureInfo.InvariantCulture, out _)))
        {
            throw new InvalidUserOperationException("BBOX 'minx,miny,maxx,maxy' biçiminde olmalı.");
        }

        if (!request.Srs.StartsWith("EPSG:", StringComparison.Ordinal) ||
            !int.TryParse(request.Srs[5..], out _))
        {
            throw new InvalidUserOperationException("SRS 'EPSG:xxxx' biçiminde olmalı.");
        }
    }

    /// <summary>
    /// WMS GetMap adresini kurar.
    ///
    /// Sürüm 1.1.1: WFS'teki eksen sırası gerekçesinin aynısı. WMS 1.3.0'da
    /// EPSG:4326 enlem/boylam sayılıyor ve BBOX ters yorumlanıyor; 1.1.1
    /// boylam/enlem kullanıyor ve parametrenin adı da "crs" değil "srs".
    /// </summary>
    private string BuildGetMapUrl(MapRenderDto request, int userId)
    {
        var workspace = _settings.Workspace;

        // Katman sırası alttan üste: poligon, çizgi, nokta. Ters olsaydı
        // poligon dolgusu noktaların üstünü kapatırdı.
        string[] layers = request.LayerSet == MapLayerSet.Heatmap
            ? [_settings.PointLayer]
            : [_settings.PolygonLayer, _settings.LineLayer, _settings.PointLayer];

        // Her katman kendi stilini kullanacaksa STYLES boş bırakılır -
        // GeoServer o zaman katmanın varsayılan stilini seçer.
        var styles = request.LayerSet == MapLayerSet.Heatmap
            ? _settings.HeatmapStyle
            : string.Join(",", layers.Select(_ => string.Empty));

        // Sahiplik filtresi. Birden çok katmanda filtreler noktalı virgülle
        // ayrılıyor ve katman sayısıyla birebir eşleşmesi gerekiyor.
        // Silinmiş kayıt filtresi burada yok; SQL View'ın içinde.
        var filter = string.Join(";", layers.Select(_ => $"inserted_user_id = {userId}"));

        var query = string.Join("&",
        [
            "service=WMS",
            "version=1.1.1",
            "request=GetMap",
            $"layers={Uri.EscapeDataString(string.Join(",", layers.Select(l => $"{workspace}:{l}")))}",
            $"styles={Uri.EscapeDataString(styles)}",
            $"srs={Uri.EscapeDataString(request.Srs)}",
            $"bbox={Uri.EscapeDataString(request.Bbox)}",
            $"width={request.Width}",
            $"height={request.Height}",
            "format=image/png",
            // Altlık harita görünmeye devam etsin.
            "transparent=true",
            $"cql_filter={Uri.EscapeDataString(filter)}"
        ]);

        return $"{_settings.BaseUrl.TrimEnd('/')}/{workspace}/wms?{query}";
    }

    private static string Truncate(string text, int max) =>
        text.Length <= max ? text : text[..max] + "...";
}
