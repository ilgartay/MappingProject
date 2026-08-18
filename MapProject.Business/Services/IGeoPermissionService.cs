using MapProject.Business.Dtos;
using NetTopologySuite.Geometries;

namespace MapProject.Business.Services;

public interface IGeoPermissionService
{
    /// <summary>Kullanıcıya doğrudan tanımlı alan; yoksa null.</summary>
    Task<GeoPermissionDto?> GetForUserAsync(int userId);

    /// <summary>Role tanımlı alan; yoksa null.</summary>
    Task<GeoPermissionDto?> GetForRoleAsync(int roleId);

    Task<GeoPermissionDto?> SaveForUserAsync(int userId, GeoPermissionSaveDto dto, int currentUserId);
    Task<GeoPermissionDto?> SaveForRoleAsync(int roleId, GeoPermissionSaveDto dto, int currentUserId);

    /// <summary>
    /// Kullanıcının etkin çizim alanı: kendi alanı + rollerinin alanları.
    /// Hiç tanım yoksa null döner ve bu "kısıt yok" demektir.
    /// </summary>
    Task<Geometry?> GetEffectiveAreaAsync(int userId);
}
