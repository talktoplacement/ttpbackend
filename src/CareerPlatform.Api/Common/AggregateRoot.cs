namespace CareerPlatform.Api.Common;

/// <summary>
/// Base type for aggregate roots: consistency boundaries that may raise
/// <see cref="DomainEvent"/>s. Events are appended in the order raised (Req 9.2), exposed as a
/// read-only collection callers cannot mutate except through raise/clear (Req 9.3), and can be
/// cleared to count 0 (Req 9.4). Implements <see cref="IHasDomainEvents"/> so interceptors can
/// query events without knowing <typeparamref name="TId"/>.
/// </summary>
/// <typeparam name="TId">The identifier type; must be non-null.</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>, IHasDomainEvents
    where TId : notnull
{
    private readonly List<DomainEvent> _domainEvents = new();

    /// <summary>
    /// The pending domain events in the order they were raised, exposed read-only so callers
    /// cannot mutate the collection except through <see cref="Raise"/> and
    /// <see cref="ClearDomainEvents"/> (Req 9.3).
    /// </summary>
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>Appends <paramref name="domainEvent"/> to the pending events in order (Req 9.2).</summary>
    protected void Raise(DomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    /// <summary>Removes all pending domain events, leaving a collection of count 0 (Req 9.4).</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
