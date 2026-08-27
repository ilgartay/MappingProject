using MapProject.Business.Analysis;
using MapProject.Business.Dtos;
using MapProject.Business.Exceptions;
using MapProject.Business.Geo;
using MapProject.Business.GeoServer;

namespace MapProject.Business.Services;

public class LocationAnalysisService : ILocationAnalysisService
{
    private readonly IGeoServerMapRenderer _renderer;
    private readonly IProvinceService _provinceService;

    public LocationAnalysisService(IGeoServerMapRenderer renderer, IProvinceService provinceService)
    {
        _renderer = renderer;
        _provinceService = provinceService;
    }

    public async Task<MapImage> RenderAsync(
        LocationAnalysisDto request, CancellationToken cancellationToken = default)
    {
        // Kriterler önce: alan geçerli olsa bile puanlar tutmuyorsa analiz
        // başlamamalı ve kullanıcı asıl sorunu görmeli.
        var criteria = LocationCriteria.Parse(request.Criteria);
        var areaWkt = await ResolveAreaAsync(request);

        return await _renderer.RenderLocationAnalysisAsync(
            request, criteria, areaWkt, cancellationToken);
    }

    /// <summary>
    /// Hedef bölge ya listeden seçilen il ya da haritaya çizilen poligon.
    /// İkisi birden gelirse il kazanıyor - arayüz zaten aynı anda
    /// ikisini göndermiyor, bu yalnızca belirsizliği kapatan bir kural.
    /// </summary>
    private async Task<string> ResolveAreaAsync(LocationAnalysisDto request)
    {
        if (request.ProvinceId is { } provinceId)
        {
            var province = await _provinceService.GetByIdAsync(provinceId);

            if (province?.Wkt is null)
            {
                throw new InvalidUserOperationException("Seçilen il bulunamadı.");
            }

            return province.Wkt;
        }

        if (!string.IsNullOrWhiteSpace(request.AreaWkt))
        {
            // Metni doğrulanmış geometriye çevirip yeniden yazıyoruz:
            // istemciden gelen ham metin doğrudan CQL'e gömülemez.
            return WktParser.ParseArea(request.AreaWkt).AsText();
        }

        throw new InvalidUserOperationException(
            "Hedef bölge seçilmeli: listeden bir il ya da haritada çizilen bir alan.");
    }
}
