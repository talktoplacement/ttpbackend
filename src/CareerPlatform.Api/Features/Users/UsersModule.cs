using CareerPlatform.Api.Features.Users.Service;

namespace CareerPlatform.Api.Features.Users;

public static class UsersModule
{
    public static IServiceCollection RegisterUsers(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IUserService, UserService>();
        return services;
    }
}
