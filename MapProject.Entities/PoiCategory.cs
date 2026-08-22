namespace MapProject.Entities;

/// <summary>
/// poi_category - POI kategorileri, kendi içinde ağaç yapısında.
///
/// Parent-Child ilişkisi tabloya kendi kendine bir yabancı anahtarla
/// kuruluyor: "Restoran" ve "Kafe" satırlarının parent_id'si "Yeme-İçme"
/// satırının id'sini gösteriyor. Ayrı bir üst-kategori tablosu açmak
/// yerine bunu tercih etmemizin sebebi derinliğin sabit olmaması -
/// yarın "Yeme-İçme → Restoran → Balık" da eklenebilir.
/// </summary>
public class PoiCategory : IModifiable
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Üst kategori. null ise bu bir kök kategoridir.</summary>
    public int? ParentId { get; set; }

    public PoiCategory? Parent { get; set; }

    public ICollection<PoiCategory> Children { get; set; } = [];

    public ICollection<Poi> Pois { get; set; } = [];

    // --- İzleme kolonları ---

    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsActive { get; set; } = true;
}
