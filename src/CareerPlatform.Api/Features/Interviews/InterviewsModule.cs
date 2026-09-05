using CareerPlatform.Api.Features.Interviews.Service;

namespace CareerPlatform.Api.Features.Interviews;

public static class InterviewsModule
{
    public static IServiceCollection RegisterInterviews(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IInterviewService, InterviewService>();
        services.AddScoped<IInterviewRubricService, InterviewRubricService>();
        return services;
    }
}
