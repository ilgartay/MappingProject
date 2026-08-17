namespace MapProject.Entities;

/// <summary>Kullanıcı-rol bağlantısı (çoka çok).</summary>
public class UserRole
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
}
