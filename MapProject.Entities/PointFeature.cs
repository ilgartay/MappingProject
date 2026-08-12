using NetTopologySuite.Geometries;

namespace MapProject.Entities;

/// <summary>tbl_point - haritada çizilen nokta geometrileri.</summary>
public class PointFeature
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>EPSG:4326 (WGS84, derece) olarak saklanır.</summary>
    public required Point Geometry { get; set; }

    public DateTime CreatedDate { get; set; }
}
