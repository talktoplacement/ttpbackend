using CareerPlatform.Api.Features.Cms.Service;

namespace CareerPlatform.Api.Features.Cms;

public static class CmsModule
{
    public static IServiceCollection RegisterCms(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICmsService, CmsService>();
        services.AddScoped<ICmsBannerService, CmsBannerService>();
        services.AddScoped<ICmsHomepageService, CmsHomepageService>();
        return services;
    }
}
