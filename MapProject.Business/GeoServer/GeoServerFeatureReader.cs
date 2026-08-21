using System.Globalization;
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
        var url = BuildGetFeatureUrl(layer, userId, extraCqlFilter);

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

        return collection.Select(ToDto).ToList();
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
    private string BuildGetFeatureUrl(string layer, int userId, string? extraCqlFilter)
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

        var query = string.Join("&",
        [
            "service=WFS",
            "version=1.0.0",
            "request=GetFeature",
            $"typeName={Uri.EscapeDataString($"{_settings.Workspace}:{layer}")}",
            "outputFormat=application/json",
            // Liste sırası EF'teki OrderBy(e => e.Id) ile aynı kalsın.
            "sortBy=id",
            $"cql_filter={Uri.EscapeDataString(filter)}"
        ]);

        return $"{_settings.BaseUrl.TrimEnd('/')}/{_settings.Workspace}/ows?{query}";
    }

    private static FeatureDto ToDto(IFeature feature)
    {
        var attributes = feature.Attributes;

        return new FeatureDto
        {
            Id = ReadInt(attributes, "id"),
            Name = ReadString(attributes, "name"),
            // Geometri GeoJSON'dan NTS nesnesi olarak geldi; istemciye
            // gönderdiğimiz biçim WKT olduğu için burada çeviriyoruz.
            Wkt = feature.Geometry?.AsText() ?? string.Empty,
            Color = ReadString(attributes, "color"),
            InsertedUserId = ReadInt(attributes, "inserted_user_id"),
            InsertedDate = ReadDate(attributes, "inserted_date") ?? default,
            ModifiedDate = ReadDate(attributes, "modified_date"),
            IsActive = ReadBool(attributes, "is_active")
        };
    }

    // --- Öznitelik okuma ---
    //
    // GeoJSON'da tipler gevşek: sayı JsonElement, long ya da decimal olarak
    // gelebiliyor. Her alanı tek tek cast etmek yerine ortak yardımcılardan
    // geçiriyoruz.

    private static object? Read(IAttributesTable attributes, string name)
    {
        return attributes.Exists(name) ? attributes[name] : null;
    }

    private static int ReadInt(IAttributesTable attributes, string name)
    {
        var value = Read(attributes, name);

        return value switch
        {
            null => 0,
            JsonElement element => element.TryGetInt32(out var parsed) ? parsed : 0,
            _ => Convert.ToInt32(value, CultureInfo.InvariantCulture)
        };
    }

    private static string ReadString(IAttributesTable attributes, string name)
    {
        var value = Read(attributes, name);

        return value switch
        {
            null => string.Empty,
            JsonElement element => element.GetString() ?? string.Empty,
            _ => value.ToString() ?? string.Empty
        };
    }

    private static bool ReadBool(IAttributesTable attributes, string name)
    {
        var value = Read(attributes, name);

        return value switch
        {
            null => false,
            JsonElement element => element.ValueKind == JsonValueKind.True,
            _ => Convert.ToBoolean(value, CultureInfo.InvariantCulture)
        };
    }

    /// <summary>
    /// GeoServer tarihleri "2026-08-15T19:50:51.100Z" biçiminde, metin olarak
    /// gönderiyor. AdjustToUniversal olmadan yerel saate kayardı.
    /// </summary>
    private static DateTime? ReadDate(IAttributesTable attributes, string name)
    {
        var value = Read(attributes, name);

        var text = value switch
        {
            null => null,
            JsonElement element => element.ValueKind == JsonValueKind.String ? element.GetString() : null,
            DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
            _ => value.ToString()
        };

        if (string.IsNullOrWhiteSpace(text)) return null;

        return DateTime.TryParse(
            text, CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
            out var parsed)
            ? parsed
            : null;
    }

    private static string Truncate(string text, int max) =>
        text.Length <= max ? text : text[..max] + "...";
}
