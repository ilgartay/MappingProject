namespace MapProject.Entities;

/// <summary>
/// guzergah - bir ulaşım hattı. Durakları sıra bazlı taşıyor.
///
/// Kolon adları ödevde verildiği için Türkçe; C# tarafı İngilizce
/// kalıyor, eşleme AppDbContext'te. POI tablosundaki kalıbın aynısı.
/// </summary>
public class Route : IModifiable
{
    public int Id { get; set; }

    /// <summary>ad</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// renk - HEX (#RRGGBB). Hattı haritada ayırt etmek için; duraklar da
    /// bağlı oldukları güzergahın rengiyle çiziliyor.
    /// </summary>
    public string Color { get; set; } = "#2563eb";

    /// <summary>1-N: bir güzergahın birden çok durağı var.</summary>
    public ICollection<Stop> Stops { get; set; } = [];

    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsActive { get; set; } = true;
}
