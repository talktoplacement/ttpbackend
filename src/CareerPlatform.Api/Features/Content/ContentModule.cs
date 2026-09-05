using CareerPlatform.Api.Features.Content.Service;

namespace CareerPlatform.Api.Features.Content;

public static class ContentModule
{
    public static IServiceCollection RegisterContent(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IContentService, ContentService>();
        return services;
    }
}
