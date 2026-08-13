using MapProject.Business.Dtos;

namespace MapProject.Business.Services;

public interface IAnalysisService
{
    /// <summary>
    /// Verilen poligonla kesişen envanterleri bulur ve sayar.
    /// Poligon veritabanına kaydedilmez, sadece sorguda kullanılır.
    /// </summary>
    Task<AnalysisResultDto> IntersectAsync(AnalysisRequestDto request);
}
