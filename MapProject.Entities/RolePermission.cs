namespace MapProject.Entities;

/// <summary>Role verilen yetki. Roldeki tüm kullanıcılar bu yetkiyi kazanır.</summary>
public class RolePermission
{
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public int PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
}
