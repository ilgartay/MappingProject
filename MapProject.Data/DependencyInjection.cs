using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MapProject.Data;

/// <summary>
/// Veri katmanının kaydı. Hangi veritabanı sağlayıcısı kullanıldığı
/// (Npgsql, NetTopologySuite) API katmanını ilgilendirmesin diye burada.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddDataServices(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, o => o.UseNetTopologySuite()));

        return services;
    }
}
