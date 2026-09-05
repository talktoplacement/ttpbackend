using CareerPlatform.Api.Features.PlacementRoles.Service;

namespace CareerPlatform.Api.Features.PlacementRoles;

public static class PlacementRolesModule
{
    public static IServiceCollection RegisterPlacementRoles(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IPlacementRoleService, PlacementRoleService>();
        return services;
    }
}
