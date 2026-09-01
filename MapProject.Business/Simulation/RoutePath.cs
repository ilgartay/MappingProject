using NetTopologySuite.Geometries;

namespace MapProject.Business.Simulation;

/// <summary>
/// Bir rota çizgisi üzerinde "başlangıçtan şu kadar metre ileride
/// neredeyim" sorusunu cevaplar.
///
/// Neden NetTopologySuite'in LengthIndexedLine'ı değil: geometri
/// EPSG:4326, yani birimi derece. Derece cinsinden uzunluk enleme göre
/// değişiyor - Türkiye'de bir boylam derecesi bir enlem derecesinden
/// yaklaşık %25 kısa. Doğrudan derece uzunluğuyla ilerleseydik araç
/// doğu-batı giderken hızlanır, kuzey-güney giderken yavaşlardı ve
/// "yüzde kaçı tamamlandı" gerçek mesafeyi göstermezdi.
///
/// Burada parça uzunlukları haversine ile metre olarak hesaplanıyor.
/// </summary>
public sealed class RoutePath
{
    private const double EarthRadiusMetres = 6_371_000;

    private readonly Coordinate[] _points;

    /// <summary>_cumulative[i] = başlangıçtan i. noktaya kadar olan metre.</summary>
    private readonly double[] _cumulative;

    public RoutePath(LineString line)
    {
        _points = line.Coordinates;
        _cumulative = new double[_points.Length];

        for (var i = 1; i < _points.Length; i++)
        {
            _cumulative[i] = _cumulative[i - 1] + DistanceMetres(_points[i - 1], _points[i]);
        }

        TotalMetres = _cumulative[^1];
    }

    public double TotalMetres { get; }

    /// <summary>
    /// Başlangıçtan verilen metre kadar ilerideki nokta. Aralık dışındaki
    /// değerler uçlara kırpılıyor.
    /// </summary>
    public Coordinate PointAt(double metres)
    {
        if (metres <= 0) return _points[0];
        if (metres >= TotalMetres) return _points[^1];

        // Hangi parçanın içindeyiz: kümülatif dizide ikili arama.
        var index = Array.BinarySearch(_cumulative, metres);
        if (index < 0) index = ~index - 1;

        var from = _points[index];
        var to = _points[index + 1];
        var segmentLength = _cumulative[index + 1] - _cumulative[index];

        // Aynı noktanın iki kez geçtiği bozuk veri: sıfıra bölmeyelim.
        if (segmentLength <= 0) return from;

        var ratio = (metres - _cumulative[index]) / segmentLength;

        return new Coordinate(
            from.X + (to.X - from.X) * ratio,
            from.Y + (to.Y - from.Y) * ratio);
    }

    /// <summary>İki coğrafi nokta arası büyük daire mesafesi (metre).</summary>
    private static double DistanceMetres(Coordinate a, Coordinate b)
    {
        var lat1 = ToRadians(a.Y);
        var lat2 = ToRadians(b.Y);
        var deltaLat = lat2 - lat1;
        var deltaLon = ToRadians(b.X - a.X);

        var h = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(lat1) * Math.Cos(lat2) *
                Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

        return 2 * EarthRadiusMetres * Math.Asin(Math.Min(1, Math.Sqrt(h)));
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
}
