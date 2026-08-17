namespace MapProject.Entities;

/// <summary>
/// Kullanıcı grubu. Yetkiler role verilir, roldeki herkes o yetkiyi kazanır.
/// </summary>
public class Role : IModifiable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public DateTime InsertedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<UserRole> UserRoles { get; set; } = [];
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
