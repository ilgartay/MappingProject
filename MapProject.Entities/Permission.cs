namespace MapProject.Entities;

/// <summary>
/// Tekil yetki tanımı, ör. "Point Ekleme".
/// Uygulamanın kontrol ettiği sabit liste; kullanıcı bunları oluşturmuyor,
/// sadece rollere ve kullanıcılara atıyor.
/// </summary>
public class Permission
{
    public int Id { get; set; }

    /// <summary>Arayüzde görünen ad: "Point Ekleme".</summary>
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Kodda kontrol edilen sabit anahtar: "point.create".
    /// Ad değişse bile kontroller bozulmasın diye ayrı tutuluyor.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    public ICollection<RolePermission> RolePermissions { get; set; } = [];
    public ICollection<UserPermission> UserPermissions { get; set; } = [];
}
