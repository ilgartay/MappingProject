using MapProject.Business.Dtos;
using MapProject.Data;
using Microsoft.EntityFrameworkCore;

namespace MapProject.Business.Services;

public class ProvinceService : IProvinceService
{
    private readonly AppDbContext _context;

    public ProvinceService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Sınır geometrisi listeye dahil değil: 81 ilin sınırı ~19 KB tutuyor
    /// ve açılır kutunun ona ihtiyacı yok. Harita yalnızca seçilen ilin
    /// sınırını çiziyor, onu da GetByIdAsync veriyor.
    /// </summary>
    public async Task<IReadOnlyList<ProvinceDto>> GetAllAsync()
    {
        return await _context.Provinces
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new ProvinceDto { Id = p.Id, Name = p.Name })
            .ToListAsync();
    }

    public async Task<ProvinceDto?> GetByIdAsync(int id)
    {
        // Geometriyi WKT'ye çevirmek .NET tarafında olmalı, o yüzden önce
        // satırı çekip sonra map'liyoruz.
        var province = await _context.Provinces
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        return province is null
            ? null
            : new ProvinceDto { Id = province.Id, Name = province.Name, Wkt = province.Geometry.AsText() };
    }
}
