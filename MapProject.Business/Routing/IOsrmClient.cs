using NetTopologySuite.Geometries;

namespace MapProject.Business.Routing;

/// <summary>OSRM'in hesapladığı rota.</summary>
/// <param name="Geometry">Yol çizgisi (EPSG:4326).</param>
/// <param name="DistanceMeters">Sürüş mesafesi, metre.</param>
/// <param name="DurationSeconds">Tahmini süre, saniye.</param>
public record OsrmRoute(LineString Geometry, double DistanceMeters, double DurationSeconds);

public interface IOsrmClient
{
    /// <summary>
    /// Verilen noktalardan sırayla geçen sürüş rotasını ister.
    /// En az iki nokta gerekiyor.
    /// </summary>
    Task<OsrmRoute> GetRouteAsync(
        IReadOnlyList<Coordinate> waypoints, CancellationToken cancellationToken = default);
}
