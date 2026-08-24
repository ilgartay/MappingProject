using MapProject.Business.Dtos;
using MapProject.Business.Exceptions;
using MapProject.Business.Geo;
using MapProject.Data;
using MapProject.Entities;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace MapProject.Business.Services;

public class GeoPermissionService : IGeoPermissionService
{
    private readonly AppDbContext _context;

    public GeoPermissionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<GeoPermissionDto?> GetForUserAsync(int userId)
    {
        var entity = await _context.GeoPermissions
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.UserId == userId);

        return entity is null ? null : ToDto(entity);
    }

    public async Task<GeoPermissionDto?> GetForRoleAsync(int roleId)
    {
        var entity = await _context.GeoPermissions
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.RoleId == roleId);

        return entity is null ? null : ToDto(entity);
    }

    public async Task<GeoPermissionDto?> SaveForUserAsync(int userId, GeoPermissionSaveDto dto, int currentUserId)
    {
        var exists = await _context.Users.AnyAsync(u => u.Id == userId && !u.IsDeleted);
        if (!exists) return null;

        var entity = await _context.GeoPermissions.FirstOrDefaultAsync(g => g.UserId == userId);
        return await ApplyAsync(entity, dto, currentUserId, userId: userId, roleId: null);
    }

    public async Task<GeoPermissionDto?> SaveForRoleAsync(int roleId, GeoPermissionSaveDto dto, int currentUserId)
    {
        var exists = await _context.Roles.AnyAsync(r => r.Id == roleId);
        if (!exists) return null;

        var entity = await _context.GeoPermissions.FirstOrDefaultAsync(g => g.RoleId == roleId);
        return await ApplyAsync(entity, dto, currentUserId, userId: null, roleId: roleId);
    }

    public async Task<Geometry?> GetEffectiveAreaAsync(int userId)
    {
        var roleIds = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        var areas = await _context.GeoPermissions
            .AsNoTracking()
            .Where(g => g.IsActive && (g.UserId == userId || (g.RoleId != null && roleIds.Contains(g.RoleId.Value))))
            .Select(g => g.Geometry)
            .ToListAsync();

        if (areas.Count == 0)
        {
            // Tanım yoksa kısıt da yok. Aksi halde alan tanımlamayı
            // unuttuğumuz herkes hiçbir yere çizemez hale gelirdi.
            return null;
        }

        // Birden fazla alan varsa birleşimi geçerli: kullanıcı kendi alanına
        // da rolünün alanına da çizebilmeli.
        Geometry union = areas[0];

        for (var i = 1; i < areas.Count; i++)
        {
            union = union.Union(areas[i]);
        }

        return union;
    }

    private async Task<GeoPermissionDto?> ApplyAsync(
        GeoPermission? entity, GeoPermissionSaveDto dto, int currentUserId, int? userId, int? roleId)
    {
        // Boş WKT = tanımlı alanı kaldır.
        if (string.IsNullOrWhiteSpace(dto.Wkt))
        {
            if (entity is not null)
            {
                entity.IsDeleted = true;
                await _context.SaveChangesAsync();
            }

            return null;
        }

        var area = WktParser.ParseArea(dto.Wkt);

        if (entity is null)
        {
            entity = new GeoPermission
            {
                Geometry = area,
                UserId = userId,
                RoleId = roleId,
                InsertedUserId = currentUserId,
                InsertedDate = DateTime.UtcNow
            };

            _context.GeoPermissions.Add(entity);
        }
        else
        {
            entity.Geometry = area;
        }

        entity.Name = dto.Name.Trim();
        await _context.SaveChangesAsync();

        return ToDto(entity);
    }

    private static GeoPermissionDto ToDto(GeoPermission entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Wkt = entity.Geometry.AsText(),
        InsertedDate = entity.InsertedDate,
        ModifiedDate = entity.ModifiedDate
    };
}

/// <summary>
/// Çizim, kullanıcıya tanımlı coğrafi alanın dışına taştığında fırlatılır.
/// </summary>
public class OutsideAllowedAreaException : InvalidUserOperationException
{
    public OutsideAllowedAreaException()
        : base("Bu çizim size tanımlı alanın dışında kalıyor.")
    {
    }
}
