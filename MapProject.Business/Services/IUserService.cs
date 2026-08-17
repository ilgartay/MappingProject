using MapProject.Business.Dtos;

namespace MapProject.Business.Services;

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> GetAllAsync();

    Task<UserDto> CreateAsync(UserSaveDto dto);

    /// <summary>Kullanıcı yoksa null döner.</summary>
    Task<UserDto?> UpdateAsync(int id, UserSaveDto dto, int currentUserId);

    /// <summary>Soft delete. Kullanıcı yoksa false döner.</summary>
    Task<bool> DeleteAsync(int id, int currentUserId);

    /// <summary>
    /// Kullanıcının durum kolonlarını günceller. Kayıt yoksa null döner.
    /// </summary>
    /// <param name="currentUserId">İsteği yapan kullanıcı; kendini kilitlemesin diye.</param>
    Task<UserDto?> UpdateStatusAsync(int id, UserStatusUpdateDto dto, int currentUserId);

    /// <summary>Yetki sekmesi: roller + her yetkinin nereden geldiği.</summary>
    Task<UserAccessDto?> GetAccessAsync(int id);

    Task<UserAccessDto?> SaveAccessAsync(int id, UserAccessSaveDto dto);

    /// <summary>Giriş yapan kullanıcının kendi etkin yetkileri.</summary>
    Task<CurrentUserDto?> GetCurrentAsync(int userId);
}
