using MapProject.Business.Dtos;
using MapProject.Business.Exceptions;
using MapProject.Data;
using MapProject.Entities;
using Microsoft.EntityFrameworkCore;

namespace MapProject.Business.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _context;

    public UserService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<UserDto>> GetAllAsync()
    {
        return await _context.Users
            .AsNoTracking()
            .OrderBy(u => u.Id)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                IsActive = u.IsActive,
                IsDeleted = u.IsDeleted,
                ModifiedDate = u.ModifiedDate
            })
            .ToListAsync();
    }

    public async Task<UserDto?> UpdateStatusAsync(int id, UserStatusUpdateDto dto, int currentUserId)
    {
        if (dto.IsActive is null && dto.IsDeleted is null)
        {
            throw new InvalidUserOperationException(
                "En az bir alan gönderin: isActive veya isDeleted.");
        }

        // AsNoTracking YOK: değişikliği takip etmesi gerekiyor, yoksa
        // SaveChanges hiçbir şey görmez ve modified_date damgalanmaz.
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

        if (user is null)
        {
            return null;
        }

        // Kendi hesabını kapatan kullanıcı bir daha giriş yapamaz.
        // Sistemde tek kullanıcı olduğu için bu uygulamayı tamamen kilitlerdi.
        if (id == currentUserId && (dto.IsActive == false || dto.IsDeleted == true))
        {
            throw new InvalidUserOperationException(
                "Kendi hesabınızı pasife alamaz veya silemezsiniz.");
        }

        if (dto.IsActive.HasValue)
        {
            user.IsActive = dto.IsActive.Value;
        }

        if (dto.IsDeleted.HasValue)
        {
            user.IsDeleted = dto.IsDeleted.Value;
        }

        // modified_date'i burada elle yazmıyoruz; AppDbContext.SaveChanges
        // değişen User satırlarına damgayı kendisi vuruyor.
        await _context.SaveChangesAsync();

        return ToDto(user);
    }

    private static UserDto ToDto(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        IsActive = user.IsActive,
        IsDeleted = user.IsDeleted,
        ModifiedDate = user.ModifiedDate
    };
}
