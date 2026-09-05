using CareerPlatform.Api.Features.Settings.Service;

namespace CareerPlatform.Api.Features.Settings;

public static class SettingsModule
{
    public static IServiceCollection RegisterSettings(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ISettingsService, SettingsService>();
        return services;
    }
}
