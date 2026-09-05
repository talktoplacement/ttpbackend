using CareerPlatform.Api.Infrastructure;
using Microsoft.Extensions.Logging;

namespace CareerPlatform.Api.BackgroundJobs.Jobs;

/// <summary>
/// Stub job for the Payments workload (Req 23.1). Resolves <see cref="IPaymentGateway"/> from
/// the per-job scope.
/// </summary>
public sealed class PaymentJob : IJob
{
    public string JobType => "Payment";

    public string JobId { get; } = Guid.NewGuid().ToString("N");

    public Task ExecuteAsync(IServiceProvider services, CancellationToken ct)
    {
        _ = services.GetService(typeof(IPaymentGateway));
        var logger = services.GetService(typeof(ILoggerFactory)) as ILoggerFactory;
        logger?.CreateLogger<PaymentJob>()
            .LogInformation("PaymentJob executed. JobId={JobId}", JobId);
        return Task.CompletedTask;
    }
}
