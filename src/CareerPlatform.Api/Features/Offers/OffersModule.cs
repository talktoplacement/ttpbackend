using CareerPlatform.Api.Features.Offers.Service;

namespace CareerPlatform.Api.Features.Offers;

/// <summary>DI wiring for the Offers feature module.</summary>
public static class OffersModule
{
    public static IServiceCollection RegisterOffers(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IOfferService, OfferService>();
        return services;
    }
}
