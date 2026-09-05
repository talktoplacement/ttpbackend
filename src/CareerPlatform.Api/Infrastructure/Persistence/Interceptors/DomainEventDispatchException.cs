namespace CareerPlatform.Api.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Raised when dispatching a <see cref="DomainEvent"/> fails after a successful commit. Surfaces
/// which event's handler failed (Req 11.4) while the already-committed transaction is left intact
/// and no already-dispatched event is re-dispatched.
/// </summary>
public sealed class DomainEventDispatchException : Exception
{
    public DomainEventDispatchException(DomainEvent domainEvent, Exception innerException)
        : base(
            $"Dispatching domain event '{domainEvent.GetType().Name}' (EventId: {domainEvent.EventId}) failed.",
            innerException)
    {
        DomainEvent = domainEvent;
    }

    /// <summary>The domain event whose handler threw during dispatch.</summary>
    public DomainEvent DomainEvent { get; }
}
