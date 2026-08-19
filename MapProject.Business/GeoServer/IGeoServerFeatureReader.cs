using MapProject.Business.Dtos;

namespace MapProject.Business.GeoServer;

/// <summary>
/// Çizim verisini GeoServer'ın WFS servisinden okur.
///
/// Bu arayüz olmasaydı FeatureService'in içinde HttpClient dururdu ve
/// katman sınırı bulanıklaşırdı: iş kuralı "kim neyi görebilir", veriyi
/// nereden çektiğimiz ise altyapı detayı.
/// </summary>
public interface IGeoServerFeatureReader
{
    /// <summary>Geometri kolonunun adı; üç tabloda da aynı.</summary>
    const string GeometryColumn = "geom";

    /// <summary>
    /// Bir katmandaki, verilen kullanıcıya ait ve silinmemiş kayıtları getirir.
    /// </summary>
    /// <param name="layer">GeoServer katman adı (ör. "tbl_point").</param>
    /// <param name="userId">Kayıtların sahibi.</param>
    /// <param name="extraCqlFilter">
    /// Sahiplik ve silinmemişlik koşullarına AND ile eklenecek ek CQL.
    /// Analiz bunu uzamsal filtre (INTERSECTS) için kullanıyor.
    /// </param>
    Task<IReadOnlyList<FeatureDto>> GetOwnedFeaturesAsync(
        string layer,
        int userId,
        string? extraCqlFilter = null,
        CancellationToken cancellationToken = default);
}
