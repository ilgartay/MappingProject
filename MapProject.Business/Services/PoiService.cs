using MapProject.Business.Dtos;
using MapProject.Business.Exceptions;
using MapProject.Business.Geo;
using MapProject.Business.GeoServer;
using MapProject.Business.Settings;
using MapProject.Data;
using MapProject.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;

namespace MapProject.Business.Services;

public class PoiService : IPoiService
{
    private readonly AppDbContext _context;
    private readonly IGeoServerFeatureReader _geoServer;
    private readonly IGeoPermissionService _geoPermissionService;
    private readonly GeoServerSettings _settings;

    public PoiService(
        AppDbContext context,
        IGeoServerFeatureReader geoServer,
        IGeoPermissionService geoPermissionService,
        IOptions<GeoServerSettings> settings)
    {
        _context = context;
        _geoServer = geoServer;
        _geoPermissionService = geoPermissionService;
        _settings = settings.Value;
    }

    /// <summary>
    /// Okuma GeoServer'ın WFS servisinden; çizimlerde olduğu gibi.
    /// vw_poi bir SQL View ve kategori ile kullanıcı adını zaten join'liyor,
    /// bu yüzden tek istekle listelenebiliyor.
    /// </summary>
    public async Task<IReadOnlyList<PoiDto>> GetAllAsync()
    {
        var records = await _geoServer.QueryAsync(_settings.PoiLayer);
        return records.Select(ToDto).ToList();
    }

    public async Task<PoiDto?> GetByIdAsync(int id)
    {
        var records = await _geoServer.QueryAsync(_settings.PoiLayer, $"id = {id}");
        return records.Select(ToDto).FirstOrDefault();
    }

    // --- Yazma: EF üzerinden, çünkü alan kontrolü ve izleme kolonları burada ---

    public async Task<PoiDto> CreateAsync(PoiSaveDto dto, int userId)
    {
        var geometry = WktParser.Parse<Point>(dto.Wkt, "POINT");

        await EnsureCategoryUsableAsync(dto.CategoryId);
        await EnsureInsideAllowedAreaAsync(geometry, userId);

        var poi = new Poi
        {
            Name = dto.Name.Trim(),
            CategoryId = dto.CategoryId,
            WorkingHours = dto.WorkingHours.Trim(),
            Geometry = geometry,
            UserId = userId,
            CreatedDate = DateTime.UtcNow,
            IsActive = dto.IsActive
        };

        _context.Pois.Add(poi);
        await _context.SaveChangesAsync();

        return await BuildDtoAsync(poi.Id);
    }

    public async Task<PoiDto?> UpdateAsync(int id, PoiSaveDto dto, int userId)
    {
        var poi = await _context.Pois.FirstOrDefaultAsync(p => p.Id == id);

        if (poi is null)
        {
            return null;
        }

        var geometry = WktParser.Parse<Point>(dto.Wkt, "POINT");

        await EnsureCategoryUsableAsync(dto.CategoryId);
        await EnsureInsideAllowedAreaAsync(geometry, userId);

        poi.Name = dto.Name.Trim();
        poi.CategoryId = dto.CategoryId;
        poi.WorkingHours = dto.WorkingHours.Trim();
        poi.Geometry = geometry;
        poi.IsActive = dto.IsActive;

        // modified_date'i elle yazmıyoruz; AppDbContext.SaveChanges damgalıyor.
        await _context.SaveChangesAsync();

        return await BuildDtoAsync(poi.Id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var poi = await _context.Pois.FirstOrDefaultAsync(p => p.Id == id);

        if (poi is null)
        {
            return false;
        }

        poi.IsDeleted = true;
        await _context.SaveChangesAsync();
        return true;
    }

    // --- Yardımcılar ---

    private static PoiDto ToDto(GeoServerRecord record) =>
        new()
        {
            Id = record.GetInt("id"),
            Name = record.GetString("isim"),
            Wkt = record.Wkt,
            CategoryId = record.GetInt("kategori_id"),
            CategoryName = record.GetString("kategori_adi"),
            CategoryPath = record.GetString("kategori_yolu"),
            WorkingHours = record.GetString("mesai_saatleri"),
            UserId = record.GetInt("user_id"),
            UserName = record.GetString("kullanici"),
            CreatedDate = record.GetDate("created_date") ?? default,
            ModifiedDate = record.GetDate("modified_date"),
            IsActive = record.GetBool("is_active")
        };

    /// <summary>
    /// Yeni kayıt GeoServer'a hemen yansıyor (aynı tabloyu okuyor), ama
    /// yine de oradan geri okuyoruz: kategori adı ve tam yol view'da
    /// hesaplanıyor, burada elle kurmak aynı mantığı iki yere yazmak olurdu.
    /// </summary>
    private async Task<PoiDto> BuildDtoAsync(int id)
    {
        return await GetByIdAsync(id)
               ?? throw new InvalidUserOperationException("Kayıt oluşturuldu ama okunamadı.");
    }

    private async Task EnsureCategoryUsableAsync(int categoryId)
    {
        var category = await _context.PoiCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == categoryId);

        if (category is null)
        {
            throw new InvalidUserOperationException("Seçilen kategori bulunamadı.");
        }

        // Pasife alınmış kategori açılır kutuda görünmüyor; yine de doğrudan
        // istek atan biri olabilir.
        if (!category.IsActive)
        {
            throw new InvalidUserOperationException($"'{category.Name}' kategorisi pasif durumda.");
        }
    }

    /// <summary>
    /// POI de haritaya konan bir işaret; coğrafi yetki kuralı çizimlerde
    /// olduğu gibi burada da geçerli.
    /// </summary>
    private async Task EnsureInsideAllowedAreaAsync(Geometry geometry, int userId)
    {
        var area = await _geoPermissionService.GetEffectiveAreaAsync(userId);

        if (area is null) return;

        if (!area.Covers(geometry))
        {
            throw new OutsideAllowedAreaException();
        }
    }
}
