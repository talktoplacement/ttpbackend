using CareerPlatform.Api.Infrastructure;
using Microsoft.Extensions.Logging;

namespace CareerPlatform.Api.BackgroundJobs.Jobs;

/// <summary>
/// Stub job for the Cleanup workload (Req 23.1). Resolves <see cref="ICacheService"/> from the
/// per-job scope for expiring/evicting stale entries.
/// </summary>
public sealed class CleanupJob : IJob
{
    public string JobType => "Cleanup";

    public string JobId { get; } = Guid.NewGuid().ToString("N");

    public Task ExecuteAsync(IServiceProvider services, CancellationToken ct)
    {
        _ = services.GetService(typeof(ICacheService));
        var logger = services.GetService(typeof(ILoggerFactory)) as ILoggerFactory;
        logger?.CreateLogger<CleanupJob>()
            .LogInformation("CleanupJob executed. JobId={JobId}", JobId);
        return Task.CompletedTask;
    }
}
