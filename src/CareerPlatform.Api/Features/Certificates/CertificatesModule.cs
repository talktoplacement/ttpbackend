using CareerPlatform.Api.Features.Certificates.Service;

namespace CareerPlatform.Api.Features.Certificates;

public static class CertificatesModule
{
    public static IServiceCollection RegisterCertificates(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICertificateService, CertificateService>();
        return services;
    }
}
