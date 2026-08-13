using System.ComponentModel.DataAnnotations;

namespace MapProject.Business.Dtos;

/// <summary>
/// Geometriyi dışarıya WKT metni olarak taşıyoruz.
/// Örnek: "POINT (32.86 39.93)", "LINESTRING (32.8 39.9, 33.1 40.2)".
/// WKT'nin içinde SRID bilgisi YOKTUR - koordinatların EPSG:4326 olduğu
/// API sözleşmesinin bir parçası, metnin kendisinden anlaşılmaz.
/// </summary>
public class FeatureDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Wkt { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
}

public class FeatureCreateDto
{
    [Required(ErrorMessage = "Ad zorunludur.")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Geometri (WKT) zorunludur.")]
    public string Wkt { get; set; } = string.Empty;

    /// <summary>
    /// HEX renk. Serbest metin kabul etmiyoruz: doğrudan CSS/harita stiline
    /// gittiği için biçimi burada sabitlemek en güvenlisi.
    /// </summary>
    [Required(ErrorMessage = "Renk zorunludur.")]
    [RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Renk #RRGGBB biçiminde olmalı.")]
    public string Color { get; set; } = string.Empty;
}

/// <summary>Haritayı tek istekle doldurabilmek için üç listeyi birlikte döner.</summary>
public class FeatureCollectionDto
{
    public IReadOnlyList<FeatureDto> Points { get; set; } = [];
    public IReadOnlyList<FeatureDto> Lines { get; set; } = [];
    public IReadOnlyList<FeatureDto> Polygons { get; set; } = [];
}
