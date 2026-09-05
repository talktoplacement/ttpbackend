using CareerPlatform.Api.Features.Assessments.Service;

namespace CareerPlatform.Api.Features.Assessments;

public static class AssessmentsModule
{
    public static IServiceCollection RegisterAssessments(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAssessmentService, AssessmentService>();
        services.AddScoped<IAssessmentRunnerService, AssessmentRunnerService>();
        services.AddScoped<IAssessmentAuthoringService, AssessmentAuthoringService>();
        return services;
    }
}
