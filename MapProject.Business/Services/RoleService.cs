using MapProject.Business.Dtos;
using MapProject.Business.Exceptions;
using MapProject.Data;
using MapProject.Entities;
using Microsoft.EntityFrameworkCore;

namespace MapProject.Business.Services;

public class RoleService : IRoleService
{
    private readonly AppDbContext _context;

    public RoleService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<RoleDto>> GetAllAsync()
    {
        // Silinmiş roller global sorgu filtresiyle zaten eleniyor.
        return await _context.Roles
            .AsNoTracking()
            .OrderBy(r => r.Id)
            .Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                IsActive = r.IsActive,
                ModifiedDate = r.ModifiedDate,
                PermissionIds = r.RolePermissions.Select(rp => rp.PermissionId).ToList(),
                UserCount = r.UserRoles.Count
            })
            .ToListAsync();
    }

    public async Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync()
    {
        return await _context.Permissions
            .AsNoTracking()
            .OrderBy(p => p.Id)
            .Select(p => new PermissionDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Description = p.Description
            })
            .ToListAsync();
    }

    public async Task<RoleDto> CreateAsync(RoleSaveDto dto)
    {
        await EnsureNameIsFreeAsync(dto.Name, null);

        var role = new Role
        {
            Name = dto.Name.Trim(),
            Description = dto.Description,
            IsActive = dto.IsActive,
            InsertedDate = DateTime.UtcNow
        };

        await ApplyPermissionsAsync(role, dto.PermissionIds);

        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        return ToDto(role, userCount: 0);
    }

    public async Task<RoleDto?> UpdateAsync(int id, RoleSaveDto dto)
    {
        var role = await _context.Roles
            .Include(r => r.RolePermissions)
            .Include(r => r.UserRoles)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (role is null)
        {
            return null;
        }

        await EnsureNameIsFreeAsync(dto.Name, id);

        role.Name = dto.Name.Trim();
        role.Description = dto.Description;
        role.IsActive = dto.IsActive;

        role.RolePermissions.Clear();
        await ApplyPermissionsAsync(role, dto.PermissionIds);

        await _context.SaveChangesAsync();

        return ToDto(role, role.UserRoles.Count);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == id);

        if (role is null)
        {
            return false;
        }

        // Soft delete: satır duruyor, sorgu filtresi gizliyor.
        // UserRoles satırları da kalıyor ama bağlantı tablosundaki filtre
        // sayesinde kimseye yetki taşımıyor.
        role.IsDeleted = true;
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Rol adı benzersiz (veritabanında da unique index var). Hatayı burada
    /// yakalayıp anlaşılır mesaj veriyoruz, yoksa 500 dönerdi.
    /// </summary>
    private async Task EnsureNameIsFreeAsync(string name, int? excludeId)
    {
        var trimmed = name.Trim();

        var exists = await _context.Roles
            .AnyAsync(r => r.Name == trimmed && (excludeId == null || r.Id != excludeId));

        if (exists)
        {
            throw new InvalidUserOperationException($"'{trimmed}' adında bir rol zaten var.");
        }
    }

    private async Task ApplyPermissionsAsync(Role role, IReadOnlyList<int> permissionIds)
    {
        if (permissionIds.Count == 0) return;

        // Var olmayan yetki id'si gönderilirse sessizce yutmuyoruz.
        var valid = await _context.Permissions
            .Where(p => permissionIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync();

        if (valid.Count != permissionIds.Distinct().Count())
        {
            throw new InvalidUserOperationException("Tanımsız yetki gönderildi.");
        }

        foreach (var permissionId in valid)
        {
            role.RolePermissions.Add(new RolePermission { PermissionId = permissionId });
        }
    }

    private static RoleDto ToDto(Role role, int userCount) => new()
    {
        Id = role.Id,
        Name = role.Name,
        Description = role.Description,
        IsActive = role.IsActive,
        ModifiedDate = role.ModifiedDate,
        PermissionIds = role.RolePermissions.Select(rp => rp.PermissionId).ToList(),
        UserCount = userCount
    };
}
