using CareerPlatform.Api.Features.Dashboard.Service;

namespace CareerPlatform.Api.Features.Dashboard;

public static class DashboardModule
{
    public static IServiceCollection RegisterDashboard(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IDashboardService, DashboardService>();
        return services;
    }
}
