using System.Globalization;
using System.Net;
using MapProject.Business.Analysis;
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
        return await FetchImageAsync(url, "harita", cancellationToken);
    }

    public Task<MapImage> RenderLocationAnalysisAsync(
        MapRenderDto request,
        IReadOnlyList<LocationCriterion> criteria,
        string areaWkt,
        CancellationToken cancellationToken = default)
    {
        Validate(request);

        // viewparams: k1..k5 kategori, a1..a5 puan. SQL View'da parametreler
        // tamsayı olarak doğrulanıyor, dolayısıyla buradan SQL sızamaz.
        var viewParams = string.Join(";", criteria
            .Select((c, i) => $"k{i + 1}:{c.CategoryId};a{i + 1}:{c.Weight}"));

        // Puanı 0 olan (seçilmeyen) kategoriler dışarıda kalsın; alan
        // kısıtı da burada. INTERSECTS alanın içine düşen POI'leri seçiyor.
        var filter = $"agirlik > 0 AND INTERSECTS(geom, {areaWkt})";

        var query = new[]
        {
            "service=WMS",
            "version=1.1.1",
            "request=GetMap",
            $"layers={Uri.EscapeDataString($"{_settings.Workspace}:{_settings.WeightedPoiLayer}")}",
            $"styles={Uri.EscapeDataString(_settings.WeightedHeatmapStyle)}",
            $"srs={Uri.EscapeDataString(request.Srs)}",
            $"bbox={Uri.EscapeDataString(request.Bbox)}",
            $"width={request.Width}",
            $"height={request.Height}",
            "format=image/png",
            "transparent=true",
            $"viewparams={Uri.EscapeDataString(viewParams)}",
            $"cql_filter={Uri.EscapeDataString(filter)}"
        };

        var url = $"{_settings.BaseUrl.TrimEnd('/')}/{_settings.Workspace}/wms?{string.Join("&", query)}";
        return FetchImageAsync(url, "konum analizi", cancellationToken);
    }

    /// <summary>
    /// GeoServer'dan resmi indirir. İki çizim yolu da (normal katmanlar ve
    /// konum analizi) aynı hata davranışını paylaşsın diye ortak.
    /// </summary>
    private async Task<MapImage> FetchImageAsync(
        string url, string what, CancellationToken cancellationToken)
    {
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
                "GeoServer WMS beklenmeyen cevap ({What}). Durum: {Status}, tip: {ContentType}, gövde: {Body}",
                what, (int)response.StatusCode, contentType, Truncate(body, 500));

            throw new GeoServerException(
                response.StatusCode == HttpStatusCode.Unauthorized
                    ? "GeoServer kimlik bilgileri reddedildi."
                    : $"Harita görüntüsü üretilemedi ({what}).");
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

        // POI, kategori başına bir stille çiziliyor: WMS aynı katmanı her
        // stil için bir kez çizip üst üste bindiriyor. Çizimlerde ise her
        // katmanın kendi varsayılan stili kullanılıyor (STYLES boş).
        //
        // Katman sırası alttan üste: poligon, çizgi, nokta. Ters olsaydı
        // poligon dolgusu noktaların üstünü kapatırdı.
        var (layers, styles) = request.LayerSet switch
        {
            MapLayerSet.Heatmap =>
                (new[] { _settings.PointLayer }, _settings.HeatmapStyle),

            MapLayerSet.Poi =>
                (_settings.PoiStyles.Select(_ => _settings.PoiLayer).ToArray(),
                 string.Join(",", _settings.PoiStyles)),

            _ => (new[] { _settings.PolygonLayer, _settings.LineLayer, _settings.PointLayer },
                  string.Join(",", Enumerable.Repeat(string.Empty, 3)))
        };

        var query = new List<string>
        {
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
            "transparent=true"
        };

        // POI'ler paylaşılan veri; sahiplik filtresi yalnızca çizimlerde var.
        // Birden çok katmanda filtreler noktalı virgülle ayrılıyor ve katman
        // sayısıyla birebir eşleşmesi gerekiyor. Silinmiş kayıt filtresi
        // burada yok; SQL View'ın içinde.
        if (request.LayerSet != MapLayerSet.Poi)
        {
            var filter = string.Join(";", layers.Select(_ => $"inserted_user_id = {userId}"));
            query.Add($"cql_filter={Uri.EscapeDataString(filter)}");
        }

        return $"{_settings.BaseUrl.TrimEnd('/')}/{workspace}/wms?{string.Join("&", query)}";
    }

    private static string Truncate(string text, int max) =>
        text.Length <= max ? text : text[..max] + "...";
}
