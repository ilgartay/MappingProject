using MapProject.Business.Dtos;

namespace MapProject.Business.Services;

public interface IRoleService
{
    Task<IReadOnlyList<RoleDto>> GetAllAsync();

    /// <summary>Uygulamanın tanıdığı tüm yetkiler; atama ekranlarını doldurur.</summary>
    Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync();

    Task<RoleDto> CreateAsync(RoleSaveDto dto);

    /// <summary>Rol yoksa null döner.</summary>
    Task<RoleDto?> UpdateAsync(int id, RoleSaveDto dto);

    /// <summary>Soft delete. Rol yoksa false döner.</summary>
    Task<bool> DeleteAsync(int id);
}
