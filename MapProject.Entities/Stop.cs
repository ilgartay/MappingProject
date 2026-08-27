using NetTopologySuite.Geometries;

namespace MapProject.Entities;

/// <summary>
/// durak - haritadaki bir duraklama noktası.
///
/// Her durak tam olarak bir güzergaha ait (1-N). Güzergahsız durak
/// tanımlı değil: bir durak hangi hatta hizmet ettiği bilinmeden
/// anlam taşımıyor, bu yüzden guzergah_id zorunlu.
/// </summary>
public class Stop : IModifiable
{
    public int Id { get; set; }

    /// <summary>ad</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>guzergah_id</summary>
    public int RouteId { get; set; }

    public Route Route { get; set; } = null!;

    /// <summary>
    /// sira - güzergah içindeki sıra numarası. Sürükle-bırak ile
    /// değiştirilen şey bu; hat çizgisi de durakları bu sıraya göre
    /// birleştiriyor.
    /// </summary>
    public int Order { get; set; }

    /// <summary>Konumu; EPSG:4326.</summary>
    public required Point Geometry { get; set; }

    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsActive { get; set; } = true;
}
