using Microsoft.EntityFrameworkCore;
using MapProject.Entities;

namespace MapProject.Data;

public class AppDbContext : DbContext
{

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {

    }
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<Permission> Permissions { get; set; } = null!;
    public DbSet<UserRole> UserRoles { get; set; } = null!;
    public DbSet<RolePermission> RolePermissions { get; set; } = null!;
    public DbSet<UserPermission> UserPermissions { get; set; } = null!;
    public DbSet<PointFeature> Points { get; set; } = null!;
    public DbSet<LineFeature> Lines { get; set; } = null!;
    public DbSet<PolygonFeature> Polygons { get; set; } = null!;
    public DbSet<GeoPermission> GeoPermissions { get; set; } = null!;
    public DbSet<PoiCategory> PoiCategories { get; set; } = null!;
    public DbSet<Poi> Pois { get; set; } = null!;

    /// <summary>
    /// Değişen kayıtlara modified_date damgası vurur.
    /// EF'in bütün kaydetme aşırı yüklemeleri bu iki metoda iniyor; damgayı
    /// burada vurunca senkron SaveChanges() çağrısı da kapsanmış oluyor.
    /// </summary>
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampModified();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        StampModified();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void StampModified()
    {
        // IModifiable uygulayan her tip kapsanıyor: User da, üç çizim tablosu da.
        foreach (var entry in ChangeTracker.Entries<IModifiable>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.ModifiedDate = DateTime.UtcNow;
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            // Aynı kullanıcı adından iki tane olmasın - veritabanı seviyesinde garanti.
            entity.HasIndex(u => u.Username).IsUnique();
            entity.Property(u => u.Username).HasMaxLength(50).IsRequired();
            entity.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();

            // Görevde istenen kolon adları snake_case; C# tarafında PascalCase
            // kalsın diye eşlemeyi burada yapıyoruz.
            entity.Property(u => u.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
            entity.Property(u => u.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(u => u.ModifiedDate).HasColumnName("modified_date");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(r => r.Name).IsUnique();
            entity.Property(r => r.Name).HasMaxLength(50).IsRequired();
            entity.Property(r => r.Description).HasMaxLength(200);
            entity.Property(r => r.InsertedDate).HasColumnName("inserted_date");
            entity.Property(r => r.ModifiedDate).HasColumnName("modified_date");
            entity.Property(r => r.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
            entity.Property(r => r.IsActive).HasColumnName("is_active").HasDefaultValue(true);

            // Silinen rol listelerde görünmesin.
            entity.HasQueryFilter(r => !r.IsDeleted);
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasIndex(p => p.Code).IsUnique();
            entity.Property(p => p.Name).HasMaxLength(100).IsRequired();
            entity.Property(p => p.Code).HasMaxLength(50).IsRequired();
            entity.Property(p => p.Description).HasMaxLength(200);
        });

        // Üç bağlantı tablosunun anahtarı iki kolondan oluşuyor; aynı
        // eşleşme iki kez eklenemesin diye birincil anahtar olarak veriliyor.
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(ur => new { ur.UserId, ur.RoleId });
            entity.HasOne(ur => ur.User).WithMany(u => u.UserRoles).HasForeignKey(ur => ur.UserId);
            entity.HasOne(ur => ur.Role).WithMany(r => r.UserRoles).HasForeignKey(ur => ur.RoleId);

            // Role'de soft delete filtresi var; bağlantı tablosunda da aynı
            // filtre olmazsa silinmiş role ait satırlar sorgularda görünür.
            entity.HasQueryFilter(ur => !ur.Role.IsDeleted);
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(rp => new { rp.RoleId, rp.PermissionId });
            entity.HasOne(rp => rp.Role).WithMany(r => r.RolePermissions).HasForeignKey(rp => rp.RoleId);
            entity.HasOne(rp => rp.Permission).WithMany(p => p.RolePermissions).HasForeignKey(rp => rp.PermissionId);
            entity.HasQueryFilter(rp => !rp.Role.IsDeleted);
        });

        modelBuilder.Entity<UserPermission>(entity =>
        {
            entity.HasKey(up => new { up.UserId, up.PermissionId });
            entity.HasOne(up => up.User).WithMany(u => u.UserPermissions).HasForeignKey(up => up.UserId);
            entity.HasOne(up => up.Permission).WithMany(p => p.UserPermissions).HasForeignKey(up => up.PermissionId);
        });

        modelBuilder.Entity<GeoPermission>(entity =>
        {
            entity.ToTable("tbl_geo_permission");
            entity.Property(g => g.Id).HasColumnName("id");
            entity.Property(g => g.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property(g => g.Color).HasColumnName("color").HasMaxLength(7)
                .IsRequired().HasDefaultValue("#009bff");
            entity.Property(g => g.Geometry).HasColumnName("geom")
                .HasColumnType("geometry(Polygon,4326)");
            entity.Property(g => g.UserId).HasColumnName("user_id");
            entity.Property(g => g.RoleId).HasColumnName("role_id");
            entity.Property(g => g.InsertedUserId).HasColumnName("inserted_user_id");
            entity.Property(g => g.InsertedDate).HasColumnName("inserted_date");
            entity.Property(g => g.ModifiedDate).HasColumnName("modified_date");
            entity.Property(g => g.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
            entity.Property(g => g.IsActive).HasColumnName("is_active").HasDefaultValue(true);

            entity.HasOne(g => g.User).WithMany().HasForeignKey(g => g.UserId);
            entity.HasOne(g => g.Role).WithMany().HasForeignKey(g => g.RoleId);

            entity.HasIndex(g => g.UserId);
            entity.HasIndex(g => g.RoleId);

            // Alan ya kullanıcıya ya role ait; ikisi birden ya da ikisi de
            // boş olamaz. Kuralı veritabanına yazıyoruz ki servis hata
            // yapsa bile tutarsız satır oluşmasın.
            entity.ToTable(t => t.HasCheckConstraint(
                "CK_geo_permission_owner",
                "(user_id IS NOT NULL AND role_id IS NULL) OR (user_id IS NULL AND role_id IS NOT NULL)"));

            entity.HasQueryFilter(g => !g.IsDeleted);
        });

        modelBuilder.Entity<PoiCategory>(entity =>
        {
            entity.ToTable("poi_category");
            entity.Property(c => c.Id).HasColumnName("id");
            entity.Property(c => c.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property(c => c.ParentId).HasColumnName("parent_id");
            entity.Property(c => c.CreatedDate).HasColumnName("created_date");
            entity.Property(c => c.ModifiedDate).HasColumnName("modified_date");
            entity.Property(c => c.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
            entity.Property(c => c.IsActive).HasColumnName("is_active").HasDefaultValue(true);

            // Kendi kendine ilişki: parent_id aynı tablonun id'sini gösteriyor.
            // Restrict seçtik - üst kategori silinince altındakiler sessizce
            // silinmesin, önce taşınmaları ya da tek tek silinmeleri gereksin.
            entity.HasOne(c => c.Parent)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(c => c.ParentId);

            // Aynı üst kategori altında aynı isimden iki tane olmasın.
            //
            // İndeks kısmi (yalnızca silinmemiş satırlar): soft delete edilen
            // bir kategori indekste yer tutmaya devam etseydi, aynı adı geri
            // eklemek isteyen yönetici servisten "boş" cevabı alıp
            // veritabanından hata yerdi. Tohumlayıcı da aynı tuzağa düşerdi.
            //
            // Kök kategorilerde parent_id NULL; PostgreSQL NULL'ları eşit
            // saymadığı için o kısmı serviste ayrıca kontrol ediyoruz.
            entity.HasIndex(c => new { c.ParentId, c.Name })
                .IsUnique()
                .HasFilter("is_deleted = false");

            entity.HasQueryFilter(c => !c.IsDeleted);
        });

        modelBuilder.Entity<Poi>(entity =>
        {
            entity.ToTable("poi");
            entity.Property(p => p.Id).HasColumnName("id");
            entity.Property(p => p.Name).HasColumnName("isim").HasMaxLength(150).IsRequired();
            entity.Property(p => p.CategoryId).HasColumnName("kategori_id");
            entity.Property(p => p.WorkingHours).HasColumnName("mesai_saatleri").HasMaxLength(100);
            entity.Property(p => p.Geometry).HasColumnName("geom")
                .HasColumnType("geometry(Point,4326)");
            entity.Property(p => p.UserId).HasColumnName("user_id");
            entity.Property(p => p.CreatedDate).HasColumnName("created_date");
            entity.Property(p => p.ModifiedDate).HasColumnName("modified_date");
            entity.Property(p => p.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
            entity.Property(p => p.IsActive).HasColumnName("is_active").HasDefaultValue(true);

            entity.HasOne(p => p.Category)
                .WithMany(c => c.Pois)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.User).WithMany().HasForeignKey(p => p.UserId);

            entity.HasIndex(p => p.CategoryId);
            entity.HasIndex(p => p.UserId);

            // PoiCategory'de soft delete filtresi var; burada da olmazsa
            // EF "filtresiz gezinme özelliği" uyarısı veriyor ve silinmiş
            // kategoriye bağlı POI'ler tutarsız görünüyor.
            entity.HasQueryFilter(p => !p.IsDeleted && !p.Category.IsDeleted);
        });

        ConfigureFeature<PointFeature>(modelBuilder, "tbl_point", "geometry(Point,4326)");
        ConfigureFeature<LineFeature>(modelBuilder, "tbl_line", "geometry(LineString,4326)");
        ConfigureFeature<PolygonFeature>(modelBuilder, "tbl_polygon", "geometry(Polygon,4326)");
    }

    /// <summary>
    /// Üç geometri tablosunun ortak ayarları. Kolon tipini
    /// "geometry(Point,4326)" gibi vermek iki şeyi veritabanına zorlatır:
    /// yanlış geometri tipi ve yanlış SRID ile kayıt eklenemez.
    /// </summary>
    private static void ConfigureFeature<TEntity>(
        ModelBuilder modelBuilder,
        string tableName,
        string geometryColumnType)
        where TEntity : class, ITrackable
    {
        modelBuilder.Entity<TEntity>(entity =>
        {
            entity.ToTable(tableName);
            entity.Property("Id").HasColumnName("id");
            entity.Property("Name").HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property("Geometry").HasColumnName("geom").HasColumnType(geometryColumnType);
            // HEX renk "#RRGGBB" - 7 karakter yeter.
            entity.Property("Color").HasColumnName("color").HasMaxLength(7)
                .IsRequired().HasDefaultValue("#009bff");

            entity.Property(e => e.InsertedUserId).HasColumnName("inserted_user_id");
            entity.Property(e => e.InsertedDate).HasColumnName("inserted_date");
            entity.Property(e => e.ModifiedDate).HasColumnName("modified_date");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);

            // Kullanıcı bazlı listeleme her sorguda bu kolonu filtreliyor.
            entity.HasIndex(e => e.InsertedUserId);

            // Global sorgu filtresi: soft delete edilmiş kayıtlar hiçbir
            // sorguda görünmez. Tek tek "where !IsDeleted" yazmayı unutma
            // riskini ortadan kaldırıyor.
            entity.HasQueryFilter(e => !e.IsDeleted);
        });
    }

}
