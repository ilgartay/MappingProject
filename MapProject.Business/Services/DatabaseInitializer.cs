using MapProject.Data;
using MapProject.Entities;
using Microsoft.EntityFrameworkCore;

namespace MapProject.Business.Services;

public class DatabaseInitializer : IDatabaseInitializer
{
    private readonly AppDbContext _context;

    public DatabaseInitializer(AppDbContext context)
    {
        _context = context;
    }

    // demo kullanıcısı, durum kolonlarını (is_active / is_deleted /
    // modified_date) canlı gösterebilmek için: admin kendi hesabını
    // kapatamıyor, deneme yapacak ikinci bir hesap gerekiyor.
    private static readonly (string Username, string Password, string Role)[] SeedUsers =
    [
        ("admin", "Admin123!", "Yönetici"),
        ("demo", "Demo123!", "Operatör")
    ];

    /// <summary>
    /// Uygulamanın tanıdığı yetkiler. Kod sabit kalır, ad değişebilir:
    /// kontroller Code üzerinden yapıldığı için etiket düzenlemesi
    /// yetkilendirmeyi bozmuyor.
    /// </summary>
    private static readonly (string Code, string Name, string Description)[] SeedPermissions =
    [
        ("point.create", "Point Ekleme", "Haritaya nokta çizebilir."),
        ("line.create", "Çizgi Ekleme", "Haritaya çizgi çizebilir."),
        ("polygon.create", "Poligon Ekleme", "Haritaya poligon çizebilir."),
        ("feature.update", "Çizim Güncelleme", "Mevcut çizimlerin bilgilerini ve konumunu değiştirebilir."),
        ("feature.delete", "Çizim Silme", "Çizimleri silebilir (soft delete)."),
        ("analysis.run", "Envanter Analizi", "Kesişim analizi çalıştırabilir."),
        ("analysis.heatmap", "Isı Haritası Analizi", "Noktaların yoğunluk haritasını görüntüleyebilir."),
        ("user.manage", "Kullanıcı Yönetimi", "Admin panelinden kullanıcıları yönetebilir."),
        ("role.manage", "Rol Yönetimi", "Admin panelinden rolleri ve yetkileri yönetebilir."),
        ("geo.manage", "Coğrafi Yetki Tanımlama", "Kullanıcı ve rollere çizim alanı tanımlayabilir.")
    ];

    private static readonly (string Name, string Description, string[] Permissions)[] SeedRoles =
    [
        ("Yönetici", "Tüm yetkiler", []), // boş dizi = hepsi, aşağıda dolduruluyor
        ("Operatör", "Çizim yapabilir, yönetim ekranlarına giremez",
            ["point.create", "line.create", "polygon.create", "feature.update", "feature.delete",
             "analysis.run", "analysis.heatmap"]),
        ("Görüntüleyici", "Sadece haritayı görüntüler", [])
    ];

    public async Task InitializeAsync()
    {
        await _context.Database.MigrateAsync();

        var permissions = await SeedPermissionsAsync();
        var roles = await SeedRolesAsync(permissions);
        await SeedUsersAsync(roles);
    }

    private async Task<Dictionary<string, Permission>> SeedPermissionsAsync()
    {
        var existing = await _context.Permissions.ToDictionaryAsync(p => p.Code);

        foreach (var (code, name, description) in SeedPermissions)
        {
            if (existing.ContainsKey(code)) continue;

            var permission = new Permission { Code = code, Name = name, Description = description };
            _context.Permissions.Add(permission);
            existing[code] = permission;
        }

        await _context.SaveChangesAsync();
        return existing;
    }

    private async Task<Dictionary<string, Role>> SeedRolesAsync(Dictionary<string, Permission> permissions)
    {
        var existing = await _context.Roles
            .Include(r => r.RolePermissions)
            .ToDictionaryAsync(r => r.Name);

        foreach (var (name, description, codes) in SeedRoles)
        {
            // Yönetici rolü zaten varsa bile sonradan eklenen yetkiler ona
            // geçsin; yoksa yeni bir yetki tanımladığımızda admin göremiyor.
            if (existing.TryGetValue(name, out var current))
            {
                if (name == "Yönetici")
                {
                    var owned = current.RolePermissions.Select(rp => rp.PermissionId).ToHashSet();

                    foreach (var permission in permissions.Values.Where(p => !owned.Contains(p.Id)))
                    {
                        current.RolePermissions.Add(new RolePermission { Permission = permission });
                    }
                }

                continue;
            }

            var role = new Role
            {
                Name = name,
                Description = description,
                InsertedDate = DateTime.UtcNow
            };

            // "Yönetici" için boş liste verdik: tüm yetkileri alsın.
            var granted = name == "Yönetici" ? permissions.Keys.ToArray() : codes;

            foreach (var code in granted)
            {
                role.RolePermissions.Add(new RolePermission { Permission = permissions[code] });
            }

            _context.Roles.Add(role);
            existing[name] = role;
        }

        await _context.SaveChangesAsync();
        return existing;
    }

    private async Task SeedUsersAsync(Dictionary<string, Role> roles)
    {
        // Kullanıcı bazında kontrol: sonradan yeni bir tohum kullanıcı
        // eklendiğinde mevcut veritabanında da oluşsun.
        var existing = await _context.Users
            .Include(u => u.UserRoles)
            .ToDictionaryAsync(u => u.Username);

        foreach (var (username, password, roleName) in SeedUsers)
        {
            if (!existing.TryGetValue(username, out var user))
            {
                // Migration'daki HasData yerine burada yapıyoruz: BCrypt hash'i her
                // seferinde farklı salt üretir, migration ise sabit veri bekler.
                user = new User
                {
                    Username = username,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
                };
                _context.Users.Add(user);
            }

            // Mevcut kullanıcıların da rolü yoksa ata: rol yapısı sonradan
            // eklendiği için eski kayıtlar rolsüz kalmasın.
            if (user.UserRoles.Count == 0 && roles.TryGetValue(roleName, out var role))
            {
                user.UserRoles.Add(new UserRole { Role = role });
            }
        }

        await _context.SaveChangesAsync();
    }
}
