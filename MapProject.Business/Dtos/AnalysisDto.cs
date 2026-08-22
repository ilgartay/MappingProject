using System.ComponentModel.DataAnnotations;

namespace MapProject.Business.Dtos;

public class AnalysisRequestDto
{
    /// <summary>Analiz poligonu, WKT (EPSG:4326). Veritabanına kaydedilmez.</summary>
    [Required(ErrorMessage = "Analiz poligonu (WKT) zorunludur.")]
    public string Wkt { get; set; } = string.Empty;

    /// <summary>
    /// Kayıtlı bir poligonun analizinde, poligonun kendisini saymamak için
    /// id'si buraya verilir. Yoksa poligon kendi kendisiyle kesişip
    /// sonucu bir fazla gösterirdi.
    /// </summary>
    public int? ExcludePolygonId { get; set; }
}

public class AnalysisItemDto
{
    /// <summary>"point" | "line" | "polygon" | "poi"</summary>
    public string Type { get; set; } = string.Empty;
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class AnalysisResultDto
{
    public int PointCount { get; set; }
    public int LineCount { get; set; }
    public int PolygonCount { get; set; }

    /// <summary>Alanın içine düşen ilgi noktası sayısı.</summary>
    public int PoiCount { get; set; }

    public int TotalCount { get; set; }

    /// <summary>Kesişen envanterlerin listesi; kullanıcıya isim göstermek için.</summary>
    public IReadOnlyList<AnalysisItemDto> Items { get; set; } = [];
}
