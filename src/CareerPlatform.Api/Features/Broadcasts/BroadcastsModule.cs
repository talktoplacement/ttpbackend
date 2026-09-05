using CareerPlatform.Api.Features.Broadcasts.Service;

namespace CareerPlatform.Api.Features.Broadcasts;

public static class BroadcastsModule
{
    public static IServiceCollection RegisterBroadcasts(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IBroadcastService, BroadcastService>();
        return services;
    }
}
