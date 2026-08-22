using MapProject.Business.Dtos;
using MapProject.Business.Exceptions;
using MapProject.Data;
using MapProject.Entities;
using Microsoft.EntityFrameworkCore;

namespace MapProject.Business.Services;

public class PoiCategoryService : IPoiCategoryService
{
    private readonly AppDbContext _context;

    public PoiCategoryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<PoiCategoryDto>> GetTreeAsync()
    {
        var all = await LoadAllAsync();

        // Ağacı bellekte kuruyoruz. Alternatifi her seviye için ayrı sorgu
        // ya da recursive CTE; kategori sayısı üç haneli kalacağı için
        // tek sorgu + bellekte birleştirme hem daha basit hem daha hızlı.
        var byParent = all
            .GroupBy(c => c.ParentId)
            .ToDictionary(g => g.Key ?? 0, g => g.ToList());

        return Build(byParent, 0);
    }

    public async Task<IReadOnlyList<PoiCategoryDto>> GetFlatAsync()
    {
        var all = await LoadAllAsync();
        var byId = all.ToDictionary(c => c.Id);

        return all
            .Select(c => new PoiCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                ParentId = c.ParentId,
                ParentName = c.ParentId is { } pid && byId.TryGetValue(pid, out var parent)
                    ? parent.Name
                    : null,
                IsActive = c.IsActive,
                PoiCount = c.PoiCount
            })
            .OrderBy(c => BuildPath(byId, c.Id))
            .ToList();
    }

    public async Task<PoiCategoryDto> CreateAsync(PoiCategorySaveDto dto)
    {
        await EnsureParentExistsAsync(dto.ParentId);
        await EnsureNameIsFreeAsync(dto.Name, dto.ParentId, null);

        var category = new PoiCategory
        {
            Name = dto.Name.Trim(),
            ParentId = dto.ParentId,
            IsActive = dto.IsActive,
            CreatedDate = DateTime.UtcNow
        };

        _context.PoiCategories.Add(category);
        await _context.SaveChangesAsync();

        return (await GetFlatAsync()).First(c => c.Id == category.Id);
    }

    public async Task<PoiCategoryDto?> UpdateAsync(int id, PoiCategorySaveDto dto)
    {
        var category = await _context.PoiCategories.FirstOrDefaultAsync(c => c.Id == id);

        if (category is null)
        {
            return null;
        }

        await EnsureParentExistsAsync(dto.ParentId);
        await EnsureNameIsFreeAsync(dto.Name, dto.ParentId, id);
        await EnsureNoCycleAsync(id, dto.ParentId);

        category.Name = dto.Name.Trim();
        category.ParentId = dto.ParentId;
        category.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();

        return (await GetFlatAsync()).FirstOrDefault(c => c.Id == id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var category = await _context.PoiCategories.FirstOrDefaultAsync(c => c.Id == id);

        if (category is null)
        {
            return false;
        }

        // Ağacı bozmamak için önce bağlı kayıtlar temizlensin. Sessizce
        // hepsini silmek yerine hata veriyoruz: yöneticinin neyi
        // kaybedeceğini bilerek karar vermesi gerekiyor.
        var childCount = await _context.PoiCategories.CountAsync(c => c.ParentId == id);

        if (childCount > 0)
        {
            throw new InvalidUserOperationException(
                $"Bu kategorinin {childCount} alt kategorisi var. Önce onları silin ya da taşıyın.");
        }

        var poiCount = await _context.Pois.CountAsync(p => p.CategoryId == id);

        if (poiCount > 0)
        {
            throw new InvalidUserOperationException(
                $"Bu kategoriye bağlı {poiCount} POI var. Önce onları başka kategoriye taşıyın.");
        }

        category.IsDeleted = true;
        await _context.SaveChangesAsync();
        return true;
    }

    // --- Yardımcılar ---

    private sealed record CategoryRow(
        int Id, string Name, int? ParentId, bool IsActive, int PoiCount);

    private async Task<List<CategoryRow>> LoadAllAsync()
    {
        return await _context.PoiCategories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryRow(
                c.Id, c.Name, c.ParentId, c.IsActive, c.Pois.Count(p => !p.IsDeleted)))
            .ToListAsync();
    }

    private static List<PoiCategoryDto> Build(Dictionary<int, List<CategoryRow>> byParent, int parentId)
    {
        if (!byParent.TryGetValue(parentId, out var children))
        {
            return [];
        }

        return children
            .Select(c => new PoiCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                ParentId = c.ParentId,
                IsActive = c.IsActive,
                PoiCount = c.PoiCount,
                Children = Build(byParent, c.Id)
            })
            .ToList();
    }

    /// <summary>"Yeme-İçme → Restoran" - sıralama ve açılır kutu etiketi için.</summary>
    private static string BuildPath(Dictionary<int, CategoryRow> byId, int id)
    {
        var parts = new List<string>();
        var current = byId.GetValueOrDefault(id);

        // Döngüye karşı sayaç: veri bozuksa sonsuza kadar dönmesin.
        for (var depth = 0; current is not null && depth < 20; depth++)
        {
            parts.Insert(0, current.Name);
            current = current.ParentId is { } pid ? byId.GetValueOrDefault(pid) : null;
        }

        return string.Join(" → ", parts);
    }

    private async Task EnsureParentExistsAsync(int? parentId)
    {
        if (parentId is null) return;

        if (!await _context.PoiCategories.AnyAsync(c => c.Id == parentId))
        {
            throw new InvalidUserOperationException("Seçilen üst kategori bulunamadı.");
        }
    }

    private async Task EnsureNameIsFreeAsync(string name, int? parentId, int? excludeId)
    {
        var trimmed = name.Trim();

        // Benzersiz indeks kök kategorilerde işe yaramıyor: PostgreSQL iki
        // NULL'ı eşit saymadığı için parent_id NULL olan satırlarda aynı ad
        // tekrar edebiliyor. O boşluğu burada kapatıyoruz.
        var exists = await _context.PoiCategories.AnyAsync(c =>
            c.Name == trimmed &&
            c.ParentId == parentId &&
            (excludeId == null || c.Id != excludeId));

        if (exists)
        {
            throw new InvalidUserOperationException(
                $"'{trimmed}' adında bir kategori aynı seviyede zaten var.");
        }
    }

    /// <summary>
    /// Kategori kendi altına taşınamaz. Olsaydı ağaçtan kopan, hiçbir kökten
    /// ulaşılamayan bir halka oluşurdu.
    /// </summary>
    private async Task EnsureNoCycleAsync(int id, int? newParentId)
    {
        if (newParentId is null) return;

        if (newParentId == id)
        {
            throw new InvalidUserOperationException("Bir kategori kendi altına taşınamaz.");
        }

        var parents = await _context.PoiCategories
            .AsNoTracking()
            .Select(c => new { c.Id, c.ParentId })
            .ToDictionaryAsync(c => c.Id, c => c.ParentId);

        var current = newParentId;

        for (var depth = 0; current is not null && depth < 20; depth++)
        {
            if (current == id)
            {
                throw new InvalidUserOperationException(
                    "Bu taşıma kategoriyi kendi alt ağacına sokuyor.");
            }

            current = parents.GetValueOrDefault(current.Value);
        }
    }
}
