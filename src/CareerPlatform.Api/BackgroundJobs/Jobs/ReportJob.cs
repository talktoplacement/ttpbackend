using CareerPlatform.Api.Infrastructure;
using Microsoft.Extensions.Logging;

namespace CareerPlatform.Api.BackgroundJobs.Jobs;

/// <summary>
/// Stub job for the Reports workload (Req 23.1). Resolves <see cref="IFileStorage"/> from the
/// per-job scope for generated report output.
/// </summary>
public sealed class ReportJob : IJob
{
    public string JobType => "Report";

    public string JobId { get; } = Guid.NewGuid().ToString("N");

    public Task ExecuteAsync(IServiceProvider services, CancellationToken ct)
    {
        _ = services.GetService(typeof(IFileStorage));
        var logger = services.GetService(typeof(ILoggerFactory)) as ILoggerFactory;
        logger?.CreateLogger<ReportJob>()
            .LogInformation("ReportJob executed. JobId={JobId}", JobId);
        return Task.CompletedTask;
    }
}
