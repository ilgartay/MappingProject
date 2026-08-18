using System.ComponentModel.DataAnnotations;

namespace MapProject.Business.Dtos;

public class PermissionDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class RoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? ModifiedDate { get; set; }

    /// <summary>Role atanmış yetkiler.</summary>
    public IReadOnlyList<int> PermissionIds { get; set; } = [];

    /// <summary>Bu rolü taşıyan kullanıcı sayısı; silmeden önce uyarı için.</summary>
    public int UserCount { get; set; }
}

public class RoleSaveDto
{
    [Required(ErrorMessage = "Rol adı zorunludur.")]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public IReadOnlyList<int> PermissionIds { get; set; } = [];
}

public class UserSaveDto
{
    [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Yeni kullanıcıda zorunlu, güncellemede boş bırakılırsa şifre değişmez.
    /// </summary>
    [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalı.")]
    public string? Password { get; set; }

    public bool IsActive { get; set; } = true;

    public IReadOnlyList<int> RoleIds { get; set; } = [];
}

/// <summary>
/// Kullanıcının yetki sekmesi. Her yetki için "rolden mi geliyor, doğrudan mı
/// verilmiş" bilgisi ayrı duruyor: arayüz rolden gelenin kutusunu kilitleyip
/// nereden geldiğini yazıyor.
/// </summary>
public class UserPermissionStateDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>Doğrudan kullanıcıya verilmiş mi.</summary>
    public bool IsDirect { get; set; }

    /// <summary>Yetkiyi sağlayan roller; boşsa rolden gelmiyor demektir.</summary>
    public IReadOnlyList<string> FromRoles { get; set; } = [];
}

public class UserAccessDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public IReadOnlyList<int> RoleIds { get; set; } = [];
    public IReadOnlyList<UserPermissionStateDto> Permissions { get; set; } = [];
}

public class UserAccessSaveDto
{
    public IReadOnlyList<int> RoleIds { get; set; } = [];

    /// <summary>Doğrudan verilecek yetkiler. Rolden gelenler burada olmasa da olur.</summary>
    public IReadOnlyList<int> PermissionIds { get; set; } = [];
}

/// <summary>Giriş yapan kullanıcının kendi bilgisi ve etkin yetkileri.</summary>
public class CurrentUserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public IReadOnlyList<string> Roles { get; set; } = [];

    /// <summary>Rollerden gelenler + doğrudan verilenler, tekilleştirilmiş kod listesi.</summary>
    public IReadOnlyList<string> Permissions { get; set; } = [];

    /// <summary>
    /// Çizim yapabileceği alan (WKT, EPSG:4326). null ise kısıt yok.
    /// Harita bunu sınır olarak çiziyor.
    /// </summary>
    public string? AllowedAreaWkt { get; set; }
}
