namespace MapProject.Business.Dtos;

/// <summary>Haritada gösterilecek katman kümesi.</summary>
public enum MapLayerSet
{
    /// <summary>Nokta, çizgi ve poligon katmanları kendi renkleriyle.</summary>
    Features,

    /// <summary>Noktaların yoğunluğundan üretilen ısı haritası.</summary>
    Heatmap,

    /// <summary>İlgi noktaları, kategorisine göre farklı ikonlarla.</summary>
    Poi
}

/// <summary>
/// WMS GetMap isteğinin istemciden gelen kısmı.
///
/// Katman adı ve filtre bilinçli olarak burada YOK: ikisini de sunucu
/// belirliyor. İstemci katman adı gönderebilseydi cql_filter'ı da
/// değiştirip başkasının çizimlerini isteyebilirdi.
/// </summary>
public class MapRenderDto
{
    public MapLayerSet LayerSet { get; set; } = MapLayerSet.Features;

    /// <summary>"minx,miny,maxx,maxy" - haritanın o anki görünen alanı.</summary>
    public string Bbox { get; set; } = string.Empty;

    public int Width { get; set; }
    public int Height { get; set; }

    /// <summary>Haritanın koordinat sistemi; OpenLayers EPSG:3857 kullanıyor.</summary>
    public string Srs { get; set; } = "EPSG:3857";
}

/// <summary>GeoServer'ın ürettiği resim.</summary>
public record MapImage(byte[] Content, string ContentType);
