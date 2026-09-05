namespace CareerPlatform.Api.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Dispatches a single <see cref="DomainEvent"/> to its handlers. Invoked by the
/// <see cref="DomainEventInterceptor"/> after a successful commit (Req 11.1).
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>Dispatches <paramref name="domainEvent"/> to its registered handlers.</summary>
    Task Dispatch(DomainEvent domainEvent, CancellationToken cancellationToken = default);
}
