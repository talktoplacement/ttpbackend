using CareerPlatform.Api.Features.PlacementPlans.Service;

namespace CareerPlatform.Api.Features.PlacementPlans;

public static class PlacementPlansModule
{
    public static IServiceCollection RegisterPlacementPlans(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IPlacementPlanService, PlacementPlanService>();
        return services;
    }
}
