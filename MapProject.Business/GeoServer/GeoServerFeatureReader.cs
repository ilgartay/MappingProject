using System.Net;
using System.Text.Json;
using MapProject.Business.Dtos;
using MapProject.Business.Exceptions;
using MapProject.Business.Geo;
using MapProject.Business.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTopologySuite;
using NetTopologySuite.Features;
using NetTopologySuite.IO.Converters;

namespace MapProject.Business.GeoServer;

/// <summary>
/// WFS GetFeature isteği atıp dönen GeoJSON'ı FeatureDto listesine çevirir.
///
/// Veri akışı artık şöyle:
///   React  ->  bizim API  ->  GeoServer (WFS)  ->  PostGIS
/// Yani API veritabanına doğrudan SELECT atmıyor; okuma işini GeoServer
/// yapıyor, biz de sonucu kendi sözleşmemize (WKT + öznitelikler) çeviriyoruz.
/// </summary>
public class GeoServerFeatureReader : IGeoServerFeatureReader
{
    private readonly HttpClient _http;
    private readonly GeoServerSettings _settings;
    private readonly ILogger<GeoServerFeatureReader> _logger;

    /// <summary>
    /// GeoJSON okurken üretilecek geometrilere 4326 damgası vursun.
    /// WktParser'daki gerekçenin aynısı: SRID 0 kalan geometri sonraki
    /// adımlarda (analiz, alan kontrolü) sessizce yanlış davranır.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var services = new NtsGeometryServices(
            NtsGeometryServices.Instance.DefaultCoordinateSequenceFactory,
            NtsGeometryServices.Instance.DefaultPrecisionModel,
            WktParser.DatabaseSrid);

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new GeoJsonConverterFactory(services.CreateGeometryFactory()));
        return options;
    }

    public GeoServerFeatureReader(
        HttpClient http,
        IOptions<GeoServerSettings> settings,
        ILogger<GeoServerFeatureReader> logger)
    {
        _http = http;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<FeatureDto>> GetOwnedFeaturesAsync(
        string layer,
        int userId,
        string? extraCqlFilter = null,
        CancellationToken cancellationToken = default)
    {
        // Silinmiş kayıt elemesi burada değil, GeoServer'daki SQL View'ın
        // içinde ("WHERE is_deleted = false"). Böylece kural tek yerde
        // duruyor: WMS de WFS de aynı view'ı okuduğu için ikisinde ayrı
        // ayrı filtre yazmak gerekmiyor.
        //
        // Sahiplik filtresi ise burada kalmak zorunda: kimin istediği
        // isteğe göre değişiyor, view'a gömülemez.
        var filter = $"inserted_user_id = {userId}";

        if (!string.IsNullOrWhiteSpace(extraCqlFilter))
        {
            filter += $" AND ({extraCqlFilter})";
        }

        var records = await QueryAsync(layer, filter, "id", cancellationToken);
        return records.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<GeoServerRecord>> QueryAsync(
        string layer,
        string? cqlFilter = null,
        string? sortBy = "id",
        CancellationToken cancellationToken = default)
    {
        var url = BuildGetFeatureUrl(layer, cqlFilter, sortBy);

        HttpResponseMessage response;

        try
        {
            response = await _http.GetAsync(url, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // Sunucu kapalı ya da cevap vermiyor. Kendi hatamızmış gibi
            // 500 dönmek yerine bunu ayrı bir tip olarak yukarı taşıyoruz.
            throw new GeoServerException($"GeoServer'a ulaşılamadı ({layer}).", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            // Gövde bize lazım: GeoServer hataları 200 değil ama açıklayıcı
            // XML döndürüyor (ör. "katman bulunamadı", "filtre okunamadı").
            _logger.LogError(
                "GeoServer {Status} döndü. Katman: {Layer}. Cevap: {Body}",
                (int)response.StatusCode, layer, Truncate(body, 500));

            throw new GeoServerException(
                response.StatusCode == HttpStatusCode.Unauthorized
                    ? "GeoServer kimlik bilgileri reddedildi."
                    : $"GeoServer katmanı okunamadı ({layer}).");
        }

        FeatureCollection? collection;

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        try
        {
            collection = await JsonSerializer.DeserializeAsync<FeatureCollection>(
                stream, JsonOptions, cancellationToken);
        }
        catch (JsonException ex)
        {
            throw new GeoServerException($"GeoServer cevabı okunamadı ({layer}).", ex);
        }

        if (collection is null)
        {
            return [];
        }

        return collection.Select(f => new GeoServerRecord(f)).ToList();
    }

    /// <summary>
    /// WFS GetFeature adresini kurar.
    ///
    /// Üç tuzak var, üçü de sessizce yanlış sonuç ürettiği için buraya yazıldı:
    ///
    /// 1) <b>Sürüm 1.0.0.</b> Daha yenisi varken eskisini kullanmamızın sebebi
    ///    koordinat ekseni sırası. WFS 1.0.0'da EPSG:4326 boylam/enlem demek;
    ///    1.1.0 ve 2.0.0'da OGC bunu otoritenin tanımına (enlem/boylam)
    ///    çevirdi. Verimiz ve GeoJSON çıktısı boylam/enlem olduğu için
    ///    2.0.0'da uzamsal filtreler hiçbir kayıtla eşleşmiyordu - hata da
    ///    vermiyordu, sadece boş sonuç dönüyordu. srsName göndermek bunu
    ///    düzeltmiyor; filtre yorumu sürüme bağlı.
    ///
    /// 2) <b>Parametre adı 1.0.0'da typeName</b> (tekil). 2.0.0'da typeNames.
    ///
    /// 3) <b>cql_filter küçük harf olmalı.</b> Bu kurulumda tamamı büyük
    ///    "CQL_FILTER" sessizce yok sayılıyor; sonuç "veri gelmedi" değil
    ///    "filtresiz tüm tablo geldi" oluyor ki çok daha tehlikeli.
    /// </summary>
    private string BuildGetFeatureUrl(string layer, string? cqlFilter, string? sortBy)
    {
        var parameters = new List<string>
        {
            "service=WFS",
            "version=1.0.0",
            "request=GetFeature",
            $"typeName={Uri.EscapeDataString($"{_settings.Workspace}:{layer}")}",
            "outputFormat=application/json"
        };

        // Liste sırası EF'teki OrderBy(e => e.Id) ile aynı kalsın.
        if (!string.IsNullOrWhiteSpace(sortBy))
        {
            parameters.Add($"sortBy={Uri.EscapeDataString(sortBy)}");
        }

        if (!string.IsNullOrWhiteSpace(cqlFilter))
        {
            parameters.Add($"cql_filter={Uri.EscapeDataString(cqlFilter)}");
        }

        return $"{_settings.BaseUrl.TrimEnd('/')}/{_settings.Workspace}/ows?{string.Join("&", parameters)}";
    }

    private static FeatureDto ToDto(GeoServerRecord record) =>
        new()
        {
            Id = record.GetInt("id"),
            Name = record.GetString("name"),
            // Geometri GeoJSON'dan NTS nesnesi olarak geldi; istemciye
            // gönderdiğimiz biçim WKT olduğu için burada çeviriyoruz.
            Wkt = record.Wkt,
            Color = record.GetString("color"),
            InsertedUserId = record.GetInt("inserted_user_id"),
            InsertedDate = record.GetDate("inserted_date") ?? default,
            ModifiedDate = record.GetDate("modified_date"),
            IsActive = record.GetBool("is_active")
        };

    private static string Truncate(string text, int max) =>
        text.Length <= max ? text : text[..max] + "...";
}
