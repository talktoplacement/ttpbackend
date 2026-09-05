using Microsoft.Extensions.Logging;

namespace CareerPlatform.Api.BackgroundJobs;

/// <summary>
/// The background worker: it dequeues jobs from <see cref="ChannelJobQueue"/> and runs each in a
/// fresh DI scope (Req 23.2). On failure a job is retried up to
/// <see cref="MaxAdditionalAttempts"/> additional times (4 tries total, Req 23.4); every failed
/// attempt is logged with the job type, id, attempt number, and error (Req 23.3); and a job that
/// exhausts all attempts is logged as permanently failed without re-enqueuing (Req 23.5).
/// </summary>
public sealed class JobProcessorHostedService : BackgroundService
{
    /// <summary>Additional retry attempts after the initial try (3 → 4 total tries, Req 23.4).</summary>
    public const int MaxAdditionalAttempts = 3;

    private readonly ChannelJobQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<JobProcessorHostedService> _logger;

    public JobProcessorHostedService(
        ChannelJobQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<JobProcessorHostedService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            await ProcessJobAsync(job, stoppingToken);
        }
    }

    /// <summary>
    /// Runs a single job with bounded retry, creating a fresh DI scope per attempt so a failed
    /// attempt cannot leak scoped state into the next. Extracted as an internal method so the
    /// retry/logging behavior is unit-testable without spinning up the full hosted-service loop.
    /// </summary>
    public async Task ProcessJobAsync(IJob job, CancellationToken ct)
    {
        var totalAttempts = MaxAdditionalAttempts + 1;

        for (var attempt = 1; attempt <= totalAttempts; attempt++)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                await job.ExecuteAsync(scope.ServiceProvider, ct);
                return;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Host is shutting down; stop retrying and surface cancellation.
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Background job failed. JobType={JobType} JobId={JobId} Attempt={Attempt}/{TotalAttempts} Error={Error}",
                    job.JobType,
                    job.JobId,
                    attempt,
                    totalAttempts,
                    ex.Message);

                if (attempt == totalAttempts)
                {
                    // Terminal state: log the permanent failure without re-enqueuing (Req 23.5).
                    _logger.LogError(
                        "Background job permanently failed after {TotalAttempts} attempts. JobType={JobType} JobId={JobId}",
                        totalAttempts,
                        job.JobType,
                        job.JobId);
                }
            }
        }
    }
}
