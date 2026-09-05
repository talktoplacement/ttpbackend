using CareerPlatform.Api.Features.Mentorship.Service;

namespace CareerPlatform.Api.Features.Mentorship;

public static class MentorshipModule
{
    public static IServiceCollection RegisterMentorship(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IMentorshipService, MentorshipService>();
        return services;
    }
}
