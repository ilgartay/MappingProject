using NetTopologySuite.Geometries;

namespace MapProject.Entities;

/// <summary>tbl_polygon - haritada çizilen geometriler.</summary>
public class PolygonFeature : ITrackable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>EPSG:4326 (WGS84, derece) olarak saklanır.</summary>
    public required Polygon Geometry { get; set; }

    /// <summary>Kullanıcının seçtiği çizim rengi, HEX (#RRGGBB).</summary>
    public string Color { get; set; } = "#009bff";

    // --- İzleme kolonları (ITrackable) ---

    /// <summary>Çizimi oluşturan kullanıcı; harita sadece kendi kayıtlarını listeliyor.</summary>
    public int InsertedUserId { get; set; }

    public DateTime InsertedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsActive { get; set; } = true;
}
