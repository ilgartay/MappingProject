using System.Net.Http.Headers;
using System.Text;
using MapProject.Business.GeoServer;
using MapProject.Business.Services;
using MapProject.Business.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MapProject.Business;

/// <summary>
/// Business katmanının servislerini tek satırda kaydetmek için.
/// Böylece API katmanı hangi sınıfın hangi arayüzü karşıladığını bilmek zorunda kalmıyor.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IFeatureService, FeatureService>();
        services.AddScoped<IAnalysisService, AnalysisService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IGeoPermissionService, GeoPermissionService>();
        services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();
        services.AddGeoServerClient();
        return services;
    }

    /// <summary>
    /// GeoServer'a giden HttpClient'i kurar.
    ///
    /// AddHttpClient (typed client) kullanmamızın sebebi: her istekte
    /// "new HttpClient()" demek soket tükenmesine yol açar, tek bir statik
    /// HttpClient ise DNS değişikliklerini kaçırır. Fabrika ikisini de çözüyor.
    /// </summary>
    private static IServiceCollection AddGeoServerClient(this IServiceCollection services)
    {
        services.AddHttpClient<IGeoServerFeatureReader, GeoServerFeatureReader>((provider, client) =>
        {
            var settings = provider.GetRequiredService<IOptions<GeoServerSettings>>().Value;

            // GeoServer REST/OWS uçları HTTP Basic ile kimlik doğruluyor.
            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{settings.Username}:{settings.Password}"));

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", credentials);

            // GeoServer kapalıysa istek sonsuza kadar beklemesin; kullanıcı
            // 100 saniye dönen bir çark yerine hızlıca hata görsün.
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        return services;
    }
}
