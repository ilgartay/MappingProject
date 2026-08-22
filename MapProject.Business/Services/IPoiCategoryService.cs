using MapProject.Business.Dtos;

namespace MapProject.Business.Services;

/// <summary>POI kategorileri; ağaç yapısında yönetiliyor.</summary>
public interface IPoiCategoryService
{
    /// <summary>Kök kategoriler, altlarındaki çocuklarla birlikte.</summary>
    Task<IReadOnlyList<PoiCategoryDto>> GetTreeAsync();

    /// <summary>
    /// Düz liste. Operatörün açılır kutusu bunu kullanıyor: ağaç yerine
    /// "Yeme-İçme → Restoran" biçiminde tek satırlık yollar.
    /// </summary>
    Task<IReadOnlyList<PoiCategoryDto>> GetFlatAsync();

    Task<PoiCategoryDto> CreateAsync(PoiCategorySaveDto dto);

    /// <summary>Kategori yoksa null döner.</summary>
    Task<PoiCategoryDto?> UpdateAsync(int id, PoiCategorySaveDto dto);

    /// <summary>Soft delete. Alt kategorisi ya da POI'si varsa hata verir.</summary>
    Task<bool> DeleteAsync(int id);
}
