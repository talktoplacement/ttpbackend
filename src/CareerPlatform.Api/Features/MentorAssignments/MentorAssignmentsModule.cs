using CareerPlatform.Api.Features.MentorAssignments.Service;

namespace CareerPlatform.Api.Features.MentorAssignments;

public static class MentorAssignmentsModule
{
    public static IServiceCollection RegisterMentorAssignments(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IMentorAssignmentService, MentorAssignmentService>();
        return services;
    }
}
