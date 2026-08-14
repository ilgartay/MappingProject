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
    private static readonly (string Username, string Password)[] SeedUsers =
    [
        ("admin", "Admin123!"),
        ("demo", "Demo123!")
    ];

    public async Task InitializeAsync()
    {
        await _context.Database.MigrateAsync();

        // Kullanıcı bazında kontrol: sonradan yeni bir tohum kullanıcı
        // eklendiğinde mevcut veritabanında da oluşsun.
        var existing = await _context.Users.Select(u => u.Username).ToListAsync();
        var missing = SeedUsers.Where(s => !existing.Contains(s.Username)).ToList();

        if (missing.Count == 0)
        {
            return;
        }

        foreach (var (username, password) in missing)
        {
            // Migration'daki HasData yerine burada yapıyoruz: BCrypt hash'i her
            // seferinde farklı salt üretir, migration ise sabit veri bekler.
            _context.Users.Add(new User
            {
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
            });
        }

        await _context.SaveChangesAsync();
    }
}
