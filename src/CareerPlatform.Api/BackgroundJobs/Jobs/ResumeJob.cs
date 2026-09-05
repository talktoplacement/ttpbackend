using CareerPlatform.Api.Infrastructure;
using Microsoft.Extensions.Logging;

namespace CareerPlatform.Api.BackgroundJobs.Jobs;

/// <summary>
/// Stub job for the Resume workload (Req 23.1). Resolves <see cref="IFileStorage"/> from the
/// per-job scope for resume artifact handling.
/// </summary>
public sealed class ResumeJob : IJob
{
    public string JobType => "Resume";

    public string JobId { get; } = Guid.NewGuid().ToString("N");

    public Task ExecuteAsync(IServiceProvider services, CancellationToken ct)
    {
        _ = services.GetService(typeof(IFileStorage));
        var logger = services.GetService(typeof(ILoggerFactory)) as ILoggerFactory;
        logger?.CreateLogger<ResumeJob>()
            .LogInformation("ResumeJob executed. JobId={JobId}", JobId);
        return Task.CompletedTask;
    }
}
