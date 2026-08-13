using MapProject.Business.Services;
using Microsoft.Extensions.DependencyInjection;

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
        return services;
    }
}
