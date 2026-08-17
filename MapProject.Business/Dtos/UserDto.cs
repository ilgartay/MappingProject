namespace MapProject.Business.Dtos;

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }

    /// <summary>Son güncelleme (UTC). Hiç değişmediyse null.</summary>
    public DateTime? ModifiedDate { get; set; }

    public IReadOnlyList<int> RoleIds { get; set; } = [];

    /// <summary>Listede okunabilir rol adları göstermek için.</summary>
    public IReadOnlyList<string> RoleNames { get; set; } = [];
}

/// <summary>
/// Kısmi güncelleme: alanlar nullable, sadece gönderilenler değişir.
/// PATCH'in PUT'tan farkı bu - tüm nesneyi göndermek gerekmiyor.
/// </summary>
public class UserStatusUpdateDto
{
    public bool? IsActive { get; set; }
    public bool? IsDeleted { get; set; }
}
