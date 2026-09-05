using CareerPlatform.Api.Infrastructure;
using Microsoft.Extensions.Logging;

namespace CareerPlatform.Api.BackgroundJobs.Jobs;

/// <summary>
/// Stub job for the Notifications workload (Req 23.1). Resolves
/// <see cref="IMessagePublisher"/> from the per-job scope.
/// </summary>
public sealed class NotificationJob : IJob
{
    public string JobType => "Notification";

    public string JobId { get; } = Guid.NewGuid().ToString("N");

    public Task ExecuteAsync(IServiceProvider services, CancellationToken ct)
    {
        _ = services.GetService(typeof(IMessagePublisher));
        var logger = services.GetService(typeof(ILoggerFactory)) as ILoggerFactory;
        logger?.CreateLogger<NotificationJob>()
            .LogInformation("NotificationJob executed. JobId={JobId}", JobId);
        return Task.CompletedTask;
    }
}
