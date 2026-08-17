namespace MapProject.Entities;

public class User : IModifiable
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;

    // Şifrenin kendisi asla saklanmaz, sadece BCrypt hash'i tutulur.
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Soft delete: kayıt silinmiş sayılır ama satır tabloda kalır.
    /// Gerçek DELETE yerine bunu kullanmak, silinen kullanıcıya bağlı
    /// geçmiş kayıtların referansını bozmaz.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Hesap askıya alınmış mı. Silmekten farkı: geri açılabilir.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Son güncelleme zamanı (UTC). İlk kayıtta null - hiç değişmemiş demek.
    /// AppDbContext.SaveChangesAsync bunu otomatik dolduruyor.
    /// </summary>
    public DateTime? ModifiedDate { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = [];

    /// <summary>Rolden bağımsız, doğrudan verilmiş yetkiler.</summary>
    public ICollection<UserPermission> UserPermissions { get; set; } = [];
}
