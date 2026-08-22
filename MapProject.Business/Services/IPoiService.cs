using MapProject.Business.Dtos;

namespace MapProject.Business.Services;

/// <summary>
/// İlgi noktaları (POI).
///
/// Çizim tablolarından farklı olarak POI'ler kullanıcıya özel değil:
/// bir restoranın konumu herkes için aynı bilgi. Bu yüzden listeleme
/// userId almıyor; kimin eklediği yalnızca bilgi olarak taşınıyor.
/// </summary>
public interface IPoiService
{
    Task<IReadOnlyList<PoiDto>> GetAllAsync();

    Task<PoiDto?> GetByIdAsync(int id);

    Task<PoiDto> CreateAsync(PoiSaveDto dto, int userId);

    /// <summary>Kayıt yoksa null döner.</summary>
    Task<PoiDto?> UpdateAsync(int id, PoiSaveDto dto, int userId);

    /// <summary>Soft delete.</summary>
    Task<bool> DeleteAsync(int id);
}
