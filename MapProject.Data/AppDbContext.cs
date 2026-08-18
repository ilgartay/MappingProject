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
