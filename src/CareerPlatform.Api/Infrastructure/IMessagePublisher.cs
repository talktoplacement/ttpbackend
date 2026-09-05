namespace CareerPlatform.Api.Infrastructure;

/// <summary>
/// A messaging abstraction for publishing messages to a broker/bus. The default registration
/// logs the message; a real transport can be substituted with a registration-only change
/// (Req 17.1, 17.3).
/// </summary>
public interface IMessagePublisher
{
    /// <summary>Publishes <paramref name="message"/> of type <typeparamref name="T"/>.</summary>
    Task PublishAsync<T>(T message, CancellationToken ct);
}
