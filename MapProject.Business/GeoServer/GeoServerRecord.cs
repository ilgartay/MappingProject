using System.Globalization;
using System.Text.Json;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace MapProject.Business.GeoServer;

/// <summary>
/// GeoServer'dan gelen tek bir kayıt: geometri + öznitelikler.
///
/// Neden ham IFeature'ı dolaştırmıyoruz: GeoJSON'da tipler gevşek. Aynı
/// sayı JsonElement, long ya da decimal olarak gelebiliyor; her çağıran
/// yerde cast yazmak yerine okuma işini burada topluyoruz.
/// </summary>
public sealed class GeoServerRecord
{
    private readonly IAttributesTable _attributes;

    public GeoServerRecord(IFeature feature)
    {
        Geometry = feature.Geometry;
        _attributes = feature.Attributes;
    }

    public Geometry? Geometry { get; }

    /// <summary>Geometrinin WKT karşılığı; API dışarıya bu biçimde veriyor.</summary>
    public string Wkt => Geometry?.AsText() ?? string.Empty;

    private object? Read(string name) => _attributes.Exists(name) ? _attributes[name] : null;

    public int GetInt(string name) => Read(name) switch
    {
        null => 0,
        JsonElement element => element.TryGetInt32(out var parsed) ? parsed : 0,
        var value => Convert.ToInt32(value, CultureInfo.InvariantCulture)
    };

    public string GetString(string name) => Read(name) switch
    {
        null => string.Empty,
        JsonElement element => element.GetString() ?? string.Empty,
        var value => value.ToString() ?? string.Empty
    };

    public bool GetBool(string name) => Read(name) switch
    {
        null => false,
        JsonElement element => element.ValueKind == JsonValueKind.True,
        var value => Convert.ToBoolean(value, CultureInfo.InvariantCulture)
    };

    /// <summary>
    /// GeoServer tarihleri "2026-08-15T19:50:51.100Z" biçiminde, metin olarak
    /// gönderiyor. AdjustToUniversal olmadan yerel saate kayardı.
    /// </summary>
    public DateTime? GetDate(string name)
    {
        var text = Read(name) switch
        {
            null => null,
            JsonElement element => element.ValueKind == JsonValueKind.String ? element.GetString() : null,
            DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
            var value => value.ToString()
        };

        if (string.IsNullOrWhiteSpace(text)) return null;

        return DateTime.TryParse(
            text, CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
            out var parsed)
            ? parsed
            : null;
    }
}
