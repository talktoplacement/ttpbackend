using CareerPlatform.Api.Features.LearningPaths.Service;

namespace CareerPlatform.Api.Features.LearningPaths;

public static class LearningPathsModule
{
    public static IServiceCollection RegisterLearningPaths(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ILearningPathService, LearningPathService>();
        return services;
    }
}
