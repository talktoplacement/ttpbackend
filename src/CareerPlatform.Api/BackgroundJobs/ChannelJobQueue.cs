using System.Threading.Channels;

namespace CareerPlatform.Api.BackgroundJobs;

/// <summary>
/// An in-process <see cref="IBackgroundJobScheduler"/> backed by an unbounded
/// <see cref="System.Threading.Channels.Channel{T}"/>. Registered as a singleton so producers
/// (request handlers) and the single consumer (the hosted worker) share one queue. Not durable
/// across process restarts — a documented limitation; swap in a durable backend when a workload
/// needs it (design.md §8).
/// </summary>
public sealed class ChannelJobQueue : IBackgroundJobScheduler
{
    private readonly Channel<IJob> _channel =
        Channel.CreateUnbounded<IJob>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });

    /// <summary>The reader consumed by <see cref="JobProcessorHostedService"/>.</summary>
    public ChannelReader<IJob> Reader => _channel.Reader;

    /// <inheritdoc />
    public ValueTask EnqueueAsync(IJob job, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        return _channel.Writer.WriteAsync(job, ct);
    }
}
