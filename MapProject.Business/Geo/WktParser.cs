using MapProject.Business.Exceptions;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace MapProject.Business.Geo;

/// <summary>
/// İstemciden gelen WKT metinlerini doğrulayıp geometriye çevirir.
/// Hem kayıt (FeatureService) hem analiz (AnalysisService) aynı kuralları
/// kullansın diye ortak yere alındı.
/// </summary>
public static class WktParser
{
    /// <summary>Veritabanı tarafındaki koordinat sistemi: WGS84, derece.</summary>
    public const int DatabaseSrid = 4326;

    // Varsayılan ayarları alıp sadece SRID'yi 4326 yapıyoruz: WKTReader
    // ürettiği geometrilere bu SRID'yi damgalar. Varsayılanla bıraksaydık
    // SRID 0 olurdu ve geometry(Point,4326) kolonu kaydı reddederdi.
    private static readonly NtsGeometryServices GeometryServices = new(
        NtsGeometryServices.Instance.DefaultCoordinateSequenceFactory,
        NtsGeometryServices.Instance.DefaultPrecisionModel,
        DatabaseSrid);

    /// <summary>
    /// WKT metnini istenen geometri tipine çevirir.
    /// İstemciden gelen metne güvenmiyoruz: bozuk olabilir, ya da
    /// nokta endpoint'ine poligon gönderilmiş olabilir.
    /// </summary>
    public static TGeometry Parse<TGeometry>(string wkt, string expectedType)
        where TGeometry : Geometry
    {
        Geometry parsed;

        try
        {
            parsed = new WKTReader(GeometryServices).Read(wkt);
        }
        catch (Exception ex)
        {
            throw new InvalidGeometryException($"WKT okunamadı: {ex.Message}");
        }

        if (parsed is not TGeometry typed)
        {
            throw new InvalidGeometryException(
                $"Bu işlem {expectedType} bekliyor, gelen geometri: {parsed.GeometryType}.");
        }

        if (typed.IsEmpty)
        {
            throw new InvalidGeometryException("Geometri boş olamaz.");
        }

        // Kendini kesen poligon gibi bozuk şekiller PostGIS'te sorun çıkarır.
        if (!typed.IsValid)
        {
            throw new InvalidGeometryException("Geometri geçersiz (ör. kendini kesen poligon).");
        }

        // WKTReader factory'den SRID'yi alır, yine de garantiye alıyoruz.
        typed.SRID = DatabaseSrid;
        return typed;
    }
}
