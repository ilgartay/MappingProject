using MapProject.Business.Dtos;

namespace MapProject.Business.GeoServer;

/// <summary>
/// GeoServer'ın WMS servisinden hazır resim ister.
///
/// WFS'ten farkı: WFS veriyi getirir, istemci çizer; WMS sunucuda çizip
/// PNG döner. Genel gösterim ve ısı haritası için WMS kullanıyoruz -
/// çizim/düzenleme için ise WFS, çünkü resmin üstündeki bir şekli
/// tıklayıp düzenlemek mümkün değil.
/// </summary>
public interface IGeoServerMapRenderer
{
    Task<MapImage> RenderAsync(MapRenderDto request, int userId, CancellationToken cancellationToken = default);
}
