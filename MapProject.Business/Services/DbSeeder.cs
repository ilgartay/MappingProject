using MapProject.Data;
using MapProject.Entities;
using Microsoft.EntityFrameworkCore;

namespace MapProject.Business.Services;

/// <summary>
/// Uygulama açılışında test kullanıcısını oluşturur.
/// Migration'daki HasData yerine burada yapıyoruz: BCrypt hash'i her seferinde
/// farklı salt üretir, migration ise sabit veri bekler.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.Users.AnyAsync())
        {
            return; // Zaten kullanıcı var, dokunma.
        }

        context.Users.Add(new User
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!")
        });

        await context.SaveChangesAsync();
    }
}
