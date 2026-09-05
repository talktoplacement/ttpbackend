namespace CareerPlatform.Api.Common;

/// <summary>
/// Base type for domain-significant events raised by an <see cref="AggregateRoot{TId}"/> and
/// dispatched after a successful commit (Req 9, 11).
/// </summary>
/// <param name="EventId">A unique identifier for this event instance.</param>
/// <param name="OccurredOnUtc">The UTC time at which the event occurred.</param>
public abstract record DomainEvent(Guid EventId, DateTime OccurredOnUtc);
