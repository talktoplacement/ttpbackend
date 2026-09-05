namespace CareerPlatform.Api.BackgroundJobs;

/// <summary>
/// A unit of deferred work executed off the request thread by the background job worker.
/// Implementations resolve their dependencies from the per-job DI scope passed to
/// <see cref="ExecuteAsync"/> rather than capturing request-scoped services, so a job runs
/// in an execution scope that outlives the originating HTTP request (Req 23.2).
/// </summary>
public interface IJob
{
    /// <summary>
    /// A stable, human-readable identifier for the job's workload category
    /// (e.g. "Email", "Notification"). Used for structured failure logging (Req 23.3).
    /// </summary>
    string JobType { get; }

    /// <summary>
    /// A unique identifier for this specific job instance. Used to correlate retry attempts and
    /// permanent-failure records in the logs (Req 23.3, 23.5).
    /// </summary>
    string JobId { get; }

    /// <summary>
    /// Executes the job's work using services resolved from <paramref name="services"/>, the
    /// service provider of a fresh DI scope created per job by the worker.
    /// </summary>
    Task ExecuteAsync(IServiceProvider services, CancellationToken ct);
}
