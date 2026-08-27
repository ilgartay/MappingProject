using System.ComponentModel.DataAnnotations;

namespace MapProject.Business.Dtos;

// --- Güzergah ---

public class RouteDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>HEX renk; duraklar da bu renkle çiziliyor.</summary>
    public string Color { get; set; } = string.Empty;

    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }

    /// <summary>Güzergahın durakları, sıra numarasına göre.</summary>
    public IReadOnlyList<StopDto> Stops { get; set; } = [];
}

public class RouteSaveDto
{
    [Required(ErrorMessage = "Güzergah adı zorunludur.")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Renk zorunludur.")]
    [RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Renk #RRGGBB biçiminde olmalı.")]
    public string Color { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

// --- Durak ---

public class StopDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Konum, WKT (EPSG:4326).</summary>
    public string Wkt { get; set; } = string.Empty;

    public int RouteId { get; set; }
    public string RouteName { get; set; } = string.Empty;

    /// <summary>Bağlı olduğu güzergahın rengi; haritada işaret rengi.</summary>
    public string RouteColor { get; set; } = string.Empty;

    /// <summary>Güzergah içindeki sırası (1'den başlar).</summary>
    public int Order { get; set; }

    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
}

public class StopSaveDto
{
    [Required(ErrorMessage = "Durak adı zorunludur.")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Konum (WKT) zorunludur.")]
    public string Wkt { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Güzergah seçilmelidir.")]
    public int RouteId { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Sürükle-bırak sonrası yeni sıra: durak id'leri istenen sırayla.
/// Tek tek "şu durak şu sıraya" demek yerine tüm listeyi almak, ara
/// durumda iki durağın aynı sıra numarasını taşımasını engelliyor.
/// </summary>
public class StopOrderDto
{
    public IReadOnlyList<int> StopIds { get; set; } = [];
}
