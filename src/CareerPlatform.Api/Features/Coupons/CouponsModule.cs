using CareerPlatform.Api.Features.Coupons.Service;

namespace CareerPlatform.Api.Features.Coupons;

public static class CouponsModule
{
    public static IServiceCollection RegisterCoupons(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICouponService, CouponService>();
        return services;
    }
}
