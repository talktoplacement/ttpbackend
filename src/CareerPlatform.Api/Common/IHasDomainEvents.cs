namespace CareerPlatform.Api.Common;

/// <summary>
/// Marker contract exposing an aggregate's pending domain events and the clear operation
/// without knowing its identifier type. The <c>DomainEventInterceptor</c> queries aggregates
/// through this interface to collect and clear events during SaveChanges (Req 11).
/// </summary>
public interface IHasDomainEvents
{
    /// <summary>The pending domain events, exposed as a read-only collection (Req 9.3).</summary>
    IReadOnlyCollection<DomainEvent> DomainEvents { get; }

    /// <summary>Removes all pending domain events, leaving a collection of count 0 (Req 9.4).</summary>
    void ClearDomainEvents();
}
