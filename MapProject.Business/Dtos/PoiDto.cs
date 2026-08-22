using System.ComponentModel.DataAnnotations;

namespace MapProject.Business.Dtos;

// --- Kategoriler ---

/// <summary>
/// Kategori ağacının bir düğümü. Children dolu geldiği için istemci
/// ağacı tek istekte kurabiliyor.
/// </summary>
public class PoiCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentId { get; set; }

    /// <summary>Üst kategorinin adı; listede "Yeme-İçme → Restoran" yazabilmek için.</summary>
    public string? ParentName { get; set; }

    public bool IsActive { get; set; }

    /// <summary>Bu kategoriye bağlı POI sayısı; silmeden önce uyarı için.</summary>
    public int PoiCount { get; set; }

    public IReadOnlyList<PoiCategoryDto> Children { get; set; } = [];
}

public class PoiCategorySaveDto
{
    [Required(ErrorMessage = "Kategori adı zorunludur.")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>null ise kök kategori olur.</summary>
    public int? ParentId { get; set; }

    public bool IsActive { get; set; } = true;
}

// --- POI ---

public class PoiDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Konum, WKT (EPSG:4326). Örn. "POINT (32.86 39.93)".</summary>
    public string Wkt { get; set; } = string.Empty;

    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>"Yeme-İçme → Restoran" biçiminde tam yol.</summary>
    public string CategoryPath { get; set; } = string.Empty;

    public string WorkingHours { get; set; } = string.Empty;

    /// <summary>POI'yi ekleyen kullanıcı - admin listesinde gösteriliyor.</summary>
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public bool IsActive { get; set; }
}

public class PoiSaveDto
{
    [Required(ErrorMessage = "İsim zorunludur.")]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Konum (WKT) zorunludur.")]
    public string Wkt { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Kategori seçilmelidir.")]
    public int CategoryId { get; set; }

    [MaxLength(100)]
    public string WorkingHours { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
