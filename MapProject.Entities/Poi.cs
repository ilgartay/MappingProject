using NetTopologySuite.Geometries;

namespace MapProject.Entities;

/// <summary>
/// poi - haritadaki ilgi noktaları (Point of Interest).
///
/// Kolon adları ödevde açıkça verildiği için Türkçe: isim, kategori_id,
/// mesai_saatleri. C# tarafında İngilizce kalıyorlar; eşleme AppDbContext'te.
/// Projedeki diğer tablolar inserted_user_id / inserted_date kullanıyor,
/// bu tablo ise ödevin istediği gibi user_id / created_date.
/// </summary>
public class Poi : IModifiable
{
    public int Id { get; set; }

    /// <summary>isim</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>kategori_id</summary>
    public int CategoryId { get; set; }

    public PoiCategory Category { get; set; } = null!;

    /// <summary>
    /// mesai_saatleri. Serbest metin: "09:00 - 18:00", "7/24",
    /// "Hafta içi 08-17, Cumartesi 10-14" gibi girdiler kabul ediliyor.
    /// Yapılandırılmış saat modeli ödevin istediğinden fazlası olurdu.
    /// </summary>
    public string WorkingHours { get; set; } = string.Empty;

    /// <summary>Haritadaki konumu; EPSG:4326.</summary>
    public required Point Geometry { get; set; }

    /// <summary>user_id - POI'yi ekleyen kullanıcı.</summary>
    public int UserId { get; set; }

    public User User { get; set; } = null!;

    // --- İzleme kolonları ---

    /// <summary>created_date</summary>
    public DateTime CreatedDate { get; set; }

    public DateTime? ModifiedDate { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsActive { get; set; } = true;
}
