using System.Globalization;
using System.Text.Json;
using MapProject.Business.Exceptions;
using MapProject.Business.Settings;
using Microsoft.Extensions.Options;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace MapProject.Business.Routing;

/// <summary>
/// OSRM'in HTTP API'sine giden istemci.
///
/// Kullandığımız uç: /route/v1/{profil}/{lon,lat};{lon,lat};...
///
/// DİKKAT - koordinat sırası: OSRM boylam,enlem bekliyor. Bu, WFS'te
/// yaşadığımız eksen sırası tuzağının aynısı ama ters yönü: orada
/// EPSG:4326'nın enlem-önce olması sorun çıkarmıştı, burada sunucu
/// açıkça boylam-önce istiyor. Ters gönderilirse hata dönmüyor,
/// "NoRoute" dönüyor ya da denizin ortasında bir rota çiziyor.
/// </summary>
public class OsrmClient : IOsrmClient
{
    private readonly HttpClient _http;
    private readonly OsrmSettings _settings;
    private readonly GeometryFactory _geometryFactory =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    public OsrmClient(HttpClient http, IOptions<OsrmSettings> settings)
    {
        _http = http;
        _settings = settings.Value;
    }

    public async Task<OsrmRoute> GetRouteAsync(
        IReadOnlyList<Coordinate> waypoints, CancellationToken cancellationToken = default)
    {
        if (waypoints.Count < 2)
        {
            throw new InvalidUserOperationException(
                "Rota hesaplamak için en az iki durak gerekiyor.");
        }

        var url = BuildUrl(waypoints);
        JsonDocument document;

        try
        {
            using var response = await _http.GetAsync(url, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                // OSRM hatayı da JSON gövdede anlatıyor; ham HTML/boş
                // gövde gelirse durum kodu tek ipucumuz.
                throw new OsrmException(
                    $"OSRM {(int)response.StatusCode} döndü: {Shorten(body)}");
            }

            document = JsonDocument.Parse(body);
        }
        catch (HttpRequestException ex)
        {
            throw new OsrmException(
                "OSRM sunucusuna ulaşılamıyor. Docker konteyneri çalışıyor mu?", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new OsrmException("OSRM zaman aşımına uğradı.", ex);
        }

        using (document)
        {
            return ReadRoute(document.RootElement);
        }
    }

    private string BuildUrl(IReadOnlyList<Coordinate> waypoints)
    {
        // InvariantCulture şart: Türkçe kültürde ondalık ayıracı virgül
        // olurdu ve "32,85" OSRM'in koordinat ayıracıyla karışırdı.
        var coordinates = string.Join(";", waypoints.Select(c =>
            string.Create(CultureInfo.InvariantCulture, $"{c.X:0.######},{c.Y:0.######}")));

        // overview=full: basitleştirilmemiş çizgi. Varsayılan "simplified"
        // uzun hatlarda yolu kesiyor ve çizgi asfalttan sapıyor.
        return $"{_settings.BaseUrl.TrimEnd('/')}/route/v1/{_settings.Profile}/{coordinates}" +
               "?overview=full&geometries=geojson&continue_straight=false";
    }

    private OsrmRoute ReadRoute(JsonElement root)
    {
        var code = root.TryGetProperty("code", out var codeElement)
            ? codeElement.GetString()
            : null;

        if (code != "Ok")
        {
            // NoRoute: noktalar yol ağına bağlanamıyor (deniz, kapsam
            // dışı ülke). Kullanıcının anlayacağı dile çeviriyoruz.
            var message = code == "NoRoute"
                ? "Duraklar arasında karayolu rotası bulunamadı. Duraklar yola yakın mı?"
                : $"OSRM rotayı hesaplayamadı ({code}).";

            throw new InvalidUserOperationException(message);
        }

        var route = root.GetProperty("routes")[0];
        var coordinates = route.GetProperty("geometry").GetProperty("coordinates")
            .EnumerateArray()
            .Select(pair => new Coordinate(pair[0].GetDouble(), pair[1].GetDouble()))
            .ToArray();

        if (coordinates.Length < 2)
        {
            throw new InvalidUserOperationException("OSRM boş bir rota döndürdü.");
        }

        return new OsrmRoute(
            _geometryFactory.CreateLineString(coordinates),
            route.GetProperty("distance").GetDouble(),
            route.GetProperty("duration").GetDouble());
    }

    private static string Shorten(string body) =>
        body.Length <= 200 ? body : body[..200] + "…";
}
