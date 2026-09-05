namespace CareerPlatform.Api.Infrastructure.Messaging;

/// <summary>
/// Placeholder <see cref="IMessagePublisher"/> that logs the published message. A real
/// transport (e.g. a broker/bus) can be substituted with a registration-only change
/// (Req 17.1, 17.2, 17.5).
/// </summary>
public sealed class LoggingMessagePublisher(ILogger<LoggingMessagePublisher> logger)
    : IMessagePublisher
{
    private readonly ILogger<LoggingMessagePublisher> _logger = logger;

    /// <inheritdoc />
    public Task PublishAsync<T>(T message, CancellationToken ct)
    {
        _logger.LogInformation(
            "Publishing message of type {MessageType}: {@Message}", typeof(T).Name, message);
        return Task.CompletedTask;
    }
}
