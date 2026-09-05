namespace CareerPlatform.Api.BackgroundJobs;

/// <summary>
/// Registers the in-process background job infrastructure (Req 23): the singleton channel-backed
/// scheduler (shared by producers and the worker) and the hosted-service worker that drains it.
/// </summary>
public static class BackgroundJobsRegistration
{
    public static IServiceCollection AddBackgroundJobs(this IServiceCollection services)
    {
        // One queue instance is shared: as the concrete ChannelJobQueue (for the worker's reader)
        // and behind the IBackgroundJobScheduler abstraction (for producers).
        services.AddSingleton<ChannelJobQueue>();
        services.AddSingleton<IBackgroundJobScheduler>(
            sp => sp.GetRequiredService<ChannelJobQueue>());

        services.AddHostedService<JobProcessorHostedService>();
        services.AddHostedService<ExpiredResumeCleanupService>();

        return services;
    }
}
