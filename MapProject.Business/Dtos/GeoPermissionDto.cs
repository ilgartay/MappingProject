using System.ComponentModel.DataAnnotations;

namespace MapProject.Business.Dtos;

/// <summary>Bir kullanıcı ya da rol için tanımlı çizim alanı.</summary>
public class GeoPermissionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Alan poligonu, WKT (EPSG:4326).</summary>
    public string Wkt { get; set; } = string.Empty;

    public DateTime InsertedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
}

public class GeoPermissionSaveDto
{
    [Required(ErrorMessage = "Alan adı zorunludur.")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Poligon WKT. Boş gönderilirse tanımlı alan kaldırılır.</summary>
    public string? Wkt { get; set; }
}
