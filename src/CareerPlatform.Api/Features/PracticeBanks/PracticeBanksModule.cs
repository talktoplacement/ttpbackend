using CareerPlatform.Api.Features.PracticeBanks.Service;

namespace CareerPlatform.Api.Features.PracticeBanks;

public static class PracticeBanksModule
{
    public static IServiceCollection RegisterPracticeBanks(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IPracticeBankService, PracticeBankService>();
        return services;
    }
}
