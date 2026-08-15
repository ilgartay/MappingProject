using MapProject.Business.Dtos;

namespace MapProject.Business.Services;

/// <summary>
/// Çizim kayıtları. Her metot userId alıyor: kullanıcı yalnızca kendi
/// çizimlerini görebilir, güncelleyebilir ve silebilir.
/// </summary>
public interface IFeatureService
{
    Task<FeatureCollectionDto> GetAllAsync(int userId);

    Task<FeatureDto> CreatePointAsync(FeatureCreateDto dto, int userId);
    Task<FeatureDto> CreateLineAsync(FeatureCreateDto dto, int userId);
    Task<FeatureDto> CreatePolygonAsync(FeatureCreateDto dto, int userId);

    /// <summary>Kayıt yoksa ya da başkasına aitse null döner.</summary>
    Task<FeatureDto?> UpdatePointAsync(int id, FeatureUpdateDto dto, int userId);
    Task<FeatureDto?> UpdateLineAsync(int id, FeatureUpdateDto dto, int userId);
    Task<FeatureDto?> UpdatePolygonAsync(int id, FeatureUpdateDto dto, int userId);

    /// <summary>
    /// Soft delete: satır silinmez, is_deleted = true yapılır.
    /// Kayıt bulunup işaretlendiyse true, yoksa false döner.
    /// </summary>
    Task<bool> DeletePointAsync(int id, int userId);
    Task<bool> DeleteLineAsync(int id, int userId);
    Task<bool> DeletePolygonAsync(int id, int userId);
}
