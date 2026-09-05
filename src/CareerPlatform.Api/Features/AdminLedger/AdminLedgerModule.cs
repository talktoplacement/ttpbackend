using CareerPlatform.Api.Features.AdminLedger.Service;

namespace CareerPlatform.Api.Features.AdminLedger;

public static class AdminLedgerModule
{
    public static IServiceCollection RegisterAdminLedger(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAdminLedgerService, AdminLedgerService>();
        return services;
    }
}
