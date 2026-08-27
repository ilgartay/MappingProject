using MapProject.Business.Dtos;

namespace MapProject.Business.Services;

/// <summary>İl sınırları; konum analizinde hedef bölge seçmek için.</summary>
public interface IProvinceService
{
    /// <summary>Ada göre sıralı liste. Sınır geometrisi taşınmıyor.</summary>
    Task<IReadOnlyList<ProvinceDto>> GetAllAsync();

    /// <summary>Tek il, sınırıyla birlikte. Bulunamazsa null.</summary>
    Task<ProvinceDto?> GetByIdAsync(int id);
}
