using CareerPlatform.Api.Features.Skills.Service;

namespace CareerPlatform.Api.Features.Skills;

public static class SkillsModule
{
    public static IServiceCollection RegisterSkills(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ISkillService, SkillService>();
        return services;
    }
}
