using MapProject.Business.Dtos;

namespace MapProject.Business.Services;

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> GetAllAsync();

    /// <summary>
    /// Kullanıcının durum kolonlarını günceller. Kayıt yoksa null döner.
    /// </summary>
    /// <param name="currentUserId">İsteği yapan kullanıcı; kendini kilitlemesin diye.</param>
    Task<UserDto?> UpdateStatusAsync(int id, UserStatusUpdateDto dto, int currentUserId);
}
