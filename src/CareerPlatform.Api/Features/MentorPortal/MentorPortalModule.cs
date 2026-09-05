using CareerPlatform.Api.Features.MentorPortal.Service;

namespace CareerPlatform.Api.Features.MentorPortal;

public static class MentorPortalModule
{
    public static IServiceCollection RegisterMentorPortal(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IMentorPortalService, MentorPortalService>();
        return services;
    }
}
