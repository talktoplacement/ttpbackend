using CareerPlatform.Api.Features.Auth.Service;

namespace CareerPlatform.Api.Features.Auth;

public static class AuthModule
{
    public static IServiceCollection RegisterAuth(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}
