using CareerPlatform.Api.Features.Learning.Service;

namespace CareerPlatform.Api.Features.Learning;

public static class LearningModule
{
    public static IServiceCollection RegisterLearning(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ILearningService, LearningService>();
        return services;
    }
}
