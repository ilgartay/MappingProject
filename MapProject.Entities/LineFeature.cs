using NetTopologySuite.Geometries;

namespace MapProject.Entities;

/// <summary>tbl_line - haritada çizilen çizgi geometrileri.</summary>
public class LineFeature
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>EPSG:4326 (WGS84, derece) olarak saklanır.</summary>
    public required LineString Geometry { get; set; }

    /// <summary>Kullanıcının seçtiği çizim rengi, HEX (#RRGGBB).</summary>
    public string Color { get; set; } = "#009bff";

    public DateTime CreatedDate { get; set; }
}
