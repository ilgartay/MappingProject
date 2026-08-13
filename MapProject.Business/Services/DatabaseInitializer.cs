using MapProject.Data;
using MapProject.Entities;
using Microsoft.EntityFrameworkCore;

namespace MapProject.Business.Services;

public interface IDatabaseInitializer
{
    /// <summary>Bekleyen migration'ları uygular ve test kullanıcısını oluşturur.</summary>
    Task InitializeAsync();
}

public class DatabaseInitializer : IDatabaseInitializer
{
    private readonly AppDbContext _context;

    public DatabaseInitializer(AppDbContext context)
    {
        _context = context;
    }

    public async Task InitializeAsync()
    {
        await _context.Database.MigrateAsync();

        if (await _context.Users.AnyAsync())
        {
            return; // Zaten kullanıcı var, dokunma.
        }

        // Migration'daki HasData yerine burada yapıyoruz: BCrypt hash'i her
        // seferinde farklı salt üretir, migration ise sabit veri bekler.
        _context.Users.Add(new User
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!")
        });

        await _context.SaveChangesAsync();
    }
}
