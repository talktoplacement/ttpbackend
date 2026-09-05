using CareerPlatform.Api.Features.PlacementReadiness.Service;

namespace CareerPlatform.Api.Features.PlacementReadiness;

public static class PlacementReadinessModule
{
    public static IServiceCollection RegisterPlacementReadiness(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IReadinessService, ReadinessService>();
        return services;
    }
}
