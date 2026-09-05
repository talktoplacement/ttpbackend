using CareerPlatform.Api.Features.Payments.Service;

namespace CareerPlatform.Api.Features.Payments;

public static class PaymentsModule
{
    public static IServiceCollection RegisterPayments(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IPaymentService, PaymentService>();
        return services;
    }
}
