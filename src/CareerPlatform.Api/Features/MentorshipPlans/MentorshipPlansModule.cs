using CareerPlatform.Api.Features.MentorshipPlans.Service;

namespace CareerPlatform.Api.Features.MentorshipPlans;

public static class MentorshipPlansModule
{
    public static IServiceCollection RegisterMentorshipPlans(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IMentorshipPlanService, MentorshipPlanService>();
        return services;
    }
}
