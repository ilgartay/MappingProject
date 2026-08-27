using MapProject.Business.Dtos;

namespace MapProject.Business.Services;

/// <summary>
/// Konum analizi: seçilen alandaki POI'lerden, kriter puanlarına göre
/// ağırlıklandırılmış bir ısı haritası üretir.
/// </summary>
public interface ILocationAnalysisService
{
    Task<MapImage> RenderAsync(LocationAnalysisDto request, CancellationToken cancellationToken = default);
}
