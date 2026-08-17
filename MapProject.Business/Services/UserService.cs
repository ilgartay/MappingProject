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
            .Where(u => !u.IsDeleted)
            .OrderBy(u => u.Id)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                IsActive = u.IsActive,
                IsDeleted = u.IsDeleted,
                ModifiedDate = u.ModifiedDate,
                RoleIds = u.UserRoles.Select(ur => ur.RoleId).ToList(),
                RoleNames = u.UserRoles.Select(ur => ur.Role.Name).ToList()
            })
            .ToListAsync();
    }

    public async Task<UserDto> CreateAsync(UserSaveDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Password))
        {
            throw new InvalidUserOperationException("Yeni kullanıcı için şifre zorunludur.");
        }

        await EnsureUsernameIsFreeAsync(dto.Username, null);

        var user = new User
        {
            Username = dto.Username.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            IsActive = dto.IsActive
        };

        await ApplyRolesAsync(user, dto.RoleIds);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return await BuildDtoAsync(user.Id);
    }

    public async Task<UserDto?> UpdateAsync(int id, UserSaveDto dto, int currentUserId)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

        if (user is null)
        {
            return null;
        }

        if (id == currentUserId && !dto.IsActive)
        {
            throw new InvalidUserOperationException("Kendi hesabınızı pasife alamazsınız.");
        }

        await EnsureUsernameIsFreeAsync(dto.Username, id);

        user.Username = dto.Username.Trim();
        user.IsActive = dto.IsActive;

        // Şifre boş bırakıldıysa dokunmuyoruz: yönetici her düzenlemede
        // şifreyi yeniden yazmak zorunda kalmasın.
        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        }

        user.UserRoles.Clear();
        await ApplyRolesAsync(user, dto.RoleIds);

        await _context.SaveChangesAsync();

        return await BuildDtoAsync(user.Id);
    }

    public async Task<bool> DeleteAsync(int id, int currentUserId)
    {
        if (id == currentUserId)
        {
            throw new InvalidUserOperationException("Kendi hesabınızı silemezsiniz.");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

        if (user is null)
        {
            return false;
        }

        // Soft delete: satır duruyor, listeler ve giriş kontrolü gizliyor.
        user.IsDeleted = true;
        await _context.SaveChangesAsync();
        return true;
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
        if (id == currentUserId && (dto.IsActive == false || dto.IsDeleted == true))
        {
            throw new InvalidUserOperationException(
                "Kendi hesabınızı pasife alamaz veya silemezsiniz.");
        }

        if (dto.IsActive.HasValue) user.IsActive = dto.IsActive.Value;
        if (dto.IsDeleted.HasValue) user.IsDeleted = dto.IsDeleted.Value;

        // modified_date'i elle yazmıyoruz; AppDbContext.SaveChanges damgalıyor.
        await _context.SaveChangesAsync();

        return await BuildDtoAsync(user.Id);
    }

    public async Task<UserAccessDto?> GetAccessAsync(int id)
    {
        var user = await LoadAccessAsync(id);

        if (user is null)
        {
            return null;
        }

        var permissions = await _context.Permissions.AsNoTracking().OrderBy(p => p.Id).ToListAsync();
        var directIds = user.UserPermissions.Select(up => up.PermissionId).ToHashSet();

        // Hangi yetkiyi hangi roller sağlıyor: arayüz "rolden geliyor"
        // etiketini bu listeden yazıyor.
        var roleSources = new Dictionary<int, List<string>>();

        foreach (var userRole in user.UserRoles)
        {
            foreach (var rolePermission in userRole.Role.RolePermissions)
            {
                if (!roleSources.TryGetValue(rolePermission.PermissionId, out var names))
                {
                    names = [];
                    roleSources[rolePermission.PermissionId] = names;
                }

                names.Add(userRole.Role.Name);
            }
        }

        return new UserAccessDto
        {
            UserId = user.Id,
            Username = user.Username,
            RoleIds = user.UserRoles.Select(ur => ur.RoleId).ToList(),
            Permissions = permissions.Select(p => new UserPermissionStateDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Description = p.Description,
                IsDirect = directIds.Contains(p.Id),
                FromRoles = roleSources.TryGetValue(p.Id, out var roles) ? roles : []
            }).ToList()
        };
    }

    public async Task<UserAccessDto?> SaveAccessAsync(int id, UserAccessSaveDto dto)
    {
        var user = await LoadAccessAsync(id);

        if (user is null)
        {
            return null;
        }

        user.UserRoles.Clear();
        await ApplyRolesAsync(user, dto.RoleIds);

        // Rollerin getirdiği yetkileri ayrıca doğrudan atamıyoruz: aynı yetki
        // iki kaynaktan gelirse rol değiştiğinde hangi satırın kalacağı
        // karışır. Arayüz bunları zaten kilitli gösteriyor.
        var roleIds = dto.RoleIds.ToList();

        var fromRoles = await _context.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Select(rp => rp.PermissionId)
            .ToListAsync();

        var directIds = dto.PermissionIds.Distinct().Except(fromRoles).ToList();

        await EnsurePermissionsExistAsync(directIds);

        user.UserPermissions.Clear();

        foreach (var permissionId in directIds)
        {
            user.UserPermissions.Add(new UserPermission { PermissionId = permissionId });
        }

        await _context.SaveChangesAsync();

        return await GetAccessAsync(id);
    }

    public async Task<CurrentUserDto?> GetCurrentAsync(int userId)
    {
        var user = await LoadAccessAsync(userId);

        if (user is null)
        {
            return null;
        }

        // Etkin yetki = rollerden gelenler + doğrudan verilenler.
        var codes = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions.Select(rp => rp.Permission.Code))
            .Concat(user.UserPermissions.Select(up => up.Permission.Code))
            .Distinct()
            .OrderBy(code => code)
            .ToList();

        return new CurrentUserDto
        {
            Id = user.Id,
            Username = user.Username,
            Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList(),
            Permissions = codes
        };
    }

    // --- Ortak yardımcılar ---

    private Task<User?> LoadAccessAsync(int id)
    {
        return _context.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role).ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .Include(u => u.UserPermissions).ThenInclude(up => up.Permission)
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
    }

    private async Task EnsureUsernameIsFreeAsync(string username, int? excludeId)
    {
        var trimmed = username.Trim();

        var exists = await _context.Users
            .AnyAsync(u => u.Username == trimmed && (excludeId == null || u.Id != excludeId));

        if (exists)
        {
            throw new InvalidUserOperationException($"'{trimmed}' kullanıcı adı zaten kullanılıyor.");
        }
    }

    private async Task ApplyRolesAsync(User user, IReadOnlyList<int> roleIds)
    {
        if (roleIds.Count == 0) return;

        var valid = await _context.Roles
            .Where(r => roleIds.Contains(r.Id))
            .Select(r => r.Id)
            .ToListAsync();

        if (valid.Count != roleIds.Distinct().Count())
        {
            throw new InvalidUserOperationException("Tanımsız rol gönderildi.");
        }

        foreach (var roleId in valid)
        {
            user.UserRoles.Add(new UserRole { RoleId = roleId });
        }
    }

    private async Task EnsurePermissionsExistAsync(IReadOnlyList<int> permissionIds)
    {
        if (permissionIds.Count == 0) return;

        var count = await _context.Permissions.CountAsync(p => permissionIds.Contains(p.Id));

        if (count != permissionIds.Count)
        {
            throw new InvalidUserOperationException("Tanımsız yetki gönderildi.");
        }
    }

    private async Task<UserDto> BuildDtoAsync(int id)
    {
        return (await GetAllAsync()).First(u => u.Id == id);
    }
}
