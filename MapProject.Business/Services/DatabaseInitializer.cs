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
        ("admin", "Admin123!", AdminRole),
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
        ("geo.manage", "Coğrafi Yetki Tanımlama", "Kullanıcı ve rollere çizim alanı tanımlayabilir."),
        ("poi.create", "POI Ekleme", "Haritaya ilgi noktası (POI) ekleyebilir."),
        ("poi.manage", "POI Yönetimi", "Admin panelinden tüm POI'leri görüntüleyebilir ve yönetebilir."),
        ("category.manage", "Kategori Yönetimi", "POI kategorilerini ekleyip düzenleyebilir.")
    ];

    /// <summary>Tüm yetkiyi alan rol; adı burada tek yerde duruyor.</summary>
    private const string AdminRole = "Admin";

    private static readonly (string Name, string Description, string[] Permissions)[] SeedRoles =
    [
        (AdminRole, "Tüm yetkiler", []), // boş dizi = hepsi, aşağıda dolduruluyor
        ("Operatör", "Çizim ve POI ekleyebilir, yönetim ekranlarına giremez",
            ["point.create", "line.create", "polygon.create", "feature.update", "feature.delete",
             "analysis.run", "analysis.heatmap", "poi.create"]),
        ("Kullanıcı", "Sadece haritayı görüntüler", [])
    ];

    /// <summary>
    /// Rol adları ödevle birlikte değişti. Var olan veritabanlarında rolü
    /// silip yeniden yaratmak kullanıcı atamalarını ve yetkileri kaybettirirdi;
    /// bu yüzden yalnızca adı güncelliyoruz.
    /// </summary>
    private static readonly (string OldName, string NewName)[] RoleRenames =
    [
        ("Yönetici", AdminRole),
        ("Görüntüleyici", "Kullanıcı")
    ];

    /// <summary>
    /// Başlangıç kategorileri; ödevdeki "Yeme-İçme → Restoran, Kafe"
    /// örneğini karşılıyor. Parent null ise kök kategori.
    /// </summary>
    private static readonly (string Name, string? Parent)[] SeedCategories =
    [
        ("Yeme-İçme", null),
        ("Restoran", "Yeme-İçme"),
        ("Kafe", "Yeme-İçme"),
        ("Konaklama", null),
        ("Otel", "Konaklama"),
        ("Sağlık", null),
        ("Eczane", "Sağlık")
    ];

    public async Task InitializeAsync()
    {
        await _context.Database.MigrateAsync();

        await RenameRolesAsync();

        var (permissions, newlyAdded) = await SeedPermissionsAsync();
        var roles = await SeedRolesAsync(permissions, newlyAdded);
        await SeedUsersAsync(roles);
        await SeedCategoriesAsync();
    }

    private async Task RenameRolesAsync()
    {
        var roles = await _context.Roles.ToListAsync();

        foreach (var (oldName, newName) in RoleRenames)
        {
            var role = roles.FirstOrDefault(r => r.Name == oldName);

            // Yeni ad zaten kullanılıyorsa dokunmuyoruz: ad kolonu benzersiz,
            // iki satırı aynı ada getirmek kaydı patlatırdı.
            if (role is null || roles.Any(r => r.Name == newName)) continue;

            role.Name = newName;
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Kategori ağacını oluşturur. Üst kategoriler önce geldiği için tek
    /// geçişte kurulabiliyor; var olanlara dokunmuyoruz ki yönetici
    /// arayüzden yaptığı düzenlemeler her açılışta geri alınmasın.
    /// </summary>
    private async Task SeedCategoriesAsync()
    {
        var existing = await _context.PoiCategories.ToDictionaryAsync(c => c.Name);

        foreach (var (name, parentName) in SeedCategories)
        {
            if (existing.ContainsKey(name)) continue;

            var category = new PoiCategory
            {
                Name = name,
                CreatedDate = DateTime.UtcNow,
                Parent = parentName is null ? null : existing.GetValueOrDefault(parentName)
            };

            _context.PoiCategories.Add(category);
            existing[name] = category;
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Yetkileri oluşturur ve bu çalıştırmada <b>ilk kez</b> eklenenlerin
    /// kodlarını da döner. Bu ayrım rollerin güncellenmesinde kullanılıyor:
    /// yeni bir yetki tanımlandığında onu bekleyen rollere dağıtmak
    /// istiyoruz, ama yöneticinin elle kaldırdığı eski bir yetkiyi her
    /// açılışta geri koymak istemiyoruz.
    /// </summary>
    private async Task<(Dictionary<string, Permission> All, HashSet<string> New)> SeedPermissionsAsync()
    {
        var existing = await _context.Permissions.ToDictionaryAsync(p => p.Code);
        var added = new HashSet<string>();

        foreach (var (code, name, description) in SeedPermissions)
        {
            if (existing.ContainsKey(code)) continue;

            var permission = new Permission { Code = code, Name = name, Description = description };
            _context.Permissions.Add(permission);
            existing[code] = permission;
            added.Add(code);
        }

        await _context.SaveChangesAsync();
        return (existing, added);
    }

    private async Task<Dictionary<string, Role>> SeedRolesAsync(
        Dictionary<string, Permission> permissions,
        HashSet<string> newPermissions)
    {
        var existing = await _context.Roles
            .Include(r => r.RolePermissions)
            .ToDictionaryAsync(r => r.Name);

        foreach (var (name, description, codes) in SeedRoles)
        {
            // Rol zaten varsa yalnızca bu çalıştırmada YENİ eklenen yetkileri
            // veriyoruz. Böylece her hafta tanımlanan yeni yetki ilgili role
            // kendiliğinden geçiyor, ama yöneticinin arayüzden kaldırdığı bir
            // yetki geri gelmiyor - o yetki artık "yeni" değil.
            if (existing.TryGetValue(name, out var current))
            {
                var owned = current.RolePermissions.Select(rp => rp.PermissionId).ToHashSet();

                // Admin her yetkiyi alır; diğer roller yalnızca kendi listesindekini.
                var wanted = name == AdminRole
                    ? newPermissions
                    : newPermissions.Intersect(codes).ToHashSet();

                foreach (var code in wanted)
                {
                    var permission = permissions[code];
                    if (owned.Contains(permission.Id)) continue;

                    current.RolePermissions.Add(new RolePermission { Permission = permission });
                }

                continue;
            }

            var role = new Role
            {
                Name = name,
                Description = description,
                InsertedDate = DateTime.UtcNow
            };

            // Admin için boş liste verdik: tüm yetkileri alsın.
            var granted = name == AdminRole ? permissions.Keys.ToArray() : codes;

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
