using CareerPlatform.Api.Features.Products.Service;

namespace CareerPlatform.Api.Features.Products;

public static class ProductsModule
{
    public static IServiceCollection RegisterProducts(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IProductService, ProductService>();
        return services;
    }
}
