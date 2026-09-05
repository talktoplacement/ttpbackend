using CareerPlatform.Api.Features.PlacementCompanies.Service;

namespace CareerPlatform.Api.Features.PlacementCompanies;

public static class PlacementCompaniesModule
{
    public static IServiceCollection RegisterPlacementCompanies(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IPlacementCompanyService, PlacementCompanyService>();
        return services;
    }
}
