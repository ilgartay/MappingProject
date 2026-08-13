using Microsoft.EntityFrameworkCore;
using MapProject.Entities;

namespace MapProject.Data;

public class AppDbContext : DbContext
{

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {

    }
    public DbSet<Location> Locations { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<PointFeature> Points { get; set; } = null!;
    public DbSet<LineFeature> Lines { get; set; } = null!;
    public DbSet<PolygonFeature> Polygons { get; set; } = null!;

    /// <summary>
    /// Değişen User satırlarına modified_date damgası vurur.
    /// EF'in bütün kaydetme aşırı yüklemeleri bu iki metoda iniyor; damgayı
    /// burada vurunca senkron SaveChanges() çağrısı da kapsanmış oluyor.
    /// </summary>
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampModifiedUsers();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        StampModifiedUsers();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void StampModifiedUsers()
    {
        foreach (var entry in ChangeTracker.Entries<User>())
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

        // İlk ödevden kalan tablo. Kolonu kısıtsız "geometry" olarak açılmıştı,
        // yani PostGIS SRID'yi 0 sayıyordu; satırlar 4326 ile yazılsa bile
        // şema bunu garanti etmiyordu. Diğer üç tabloyla aynı hizaya getiriyoruz.
        modelBuilder.Entity<Location>(entity =>
        {
            entity.Property(l => l.Coordinates).HasColumnType("geometry(Point,4326)");
        });

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
        where TEntity : class
    {
        modelBuilder.Entity<TEntity>(entity =>
        {
            entity.ToTable(tableName);
            entity.Property("Id").HasColumnName("id");
            entity.Property("Name").HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property("Geometry").HasColumnName("geom").HasColumnType(geometryColumnType);
            entity.Property("CreatedDate").HasColumnName("created_date");
        });
    }

}
