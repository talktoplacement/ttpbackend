using CareerPlatform.Api.Features.Practice.Service;

namespace CareerPlatform.Api.Features.Practice;

public static class PracticeModule
{
    public static IServiceCollection RegisterPractice(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IPracticeService, PracticeService>();
        return services;
    }
}
