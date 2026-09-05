using CareerPlatform.Api.Features.SubscriptionPlans.Service;

namespace CareerPlatform.Api.Features.SubscriptionPlans;

public static class SubscriptionPlansModule
{
    public static IServiceCollection RegisterSubscriptionPlans(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ISubscriptionPlanService, SubscriptionPlanService>();
        return services;
    }
}
