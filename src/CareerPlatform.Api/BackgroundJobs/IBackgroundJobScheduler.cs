namespace CareerPlatform.Api.BackgroundJobs;

/// <summary>
/// The contract slices use to enqueue deferred work (Req 23.1). It is deliberately narrow so a
/// durable backend (e.g. Hangfire) can replace the default in-process channel implementation
/// without touching callers — see the ADR in design.md §8.
/// </summary>
public interface IBackgroundJobScheduler
{
    /// <summary>
    /// Enqueues <paramref name="job"/> for asynchronous execution by the background worker. The
    /// call returns as soon as the job is queued; the job runs later in its own DI scope
    /// (Req 23.2).
    /// </summary>
    ValueTask EnqueueAsync(IJob job, CancellationToken ct = default);
}
