using CareerPlatform.Api.Features.Support.Service;

namespace CareerPlatform.Api.Features.Support;

public static class SupportModule
{
    public static IServiceCollection RegisterSupport(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ISupportService, SupportService>();
        return services;
    }
}
