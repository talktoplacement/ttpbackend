namespace CareerPlatform.Api.Infrastructure.Persistence.Interceptors;

/// <summary>
/// In-process <see cref="IDomainEventDispatcher"/>. Resolves every registered
/// <see cref="IDomainEventHandler{TEvent}"/> whose event type matches (or is a base of) the
/// dispatched event and invokes it sequentially. When no handlers are registered the call
/// is a no-op — dispatching a domain event on a service with no subscribers is not a failure.
/// </summary>
public sealed class InProcessDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _services;

    public InProcessDomainEventDispatcher(IServiceProvider services) => _services = services;

    public async Task Dispatch(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var handlerInterface = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
        var handlerCollection = typeof(IEnumerable<>).MakeGenericType(handlerInterface);
        var handlers = (System.Collections.IEnumerable?)_services.GetService(handlerCollection);
        if (handlers is null) return;

        foreach (var handler in handlers)
        {
            if (handler is null) continue;
            var method = handler.GetType().GetMethod(nameof(IDomainEventHandler<DomainEvent>.HandleAsync));
            if (method is null) continue;
            var task = method.Invoke(handler, new object[] { domainEvent, cancellationToken }) as Task;
            if (task is not null) await task;
        }
    }
}

/// <summary>
/// Handler contract for a specific <see cref="DomainEvent"/> subtype. Register concrete
/// implementations as scoped services; the <see cref="InProcessDomainEventDispatcher"/> discovers
/// and invokes them via DI. No handlers exist today (post-migration) — the contract stays so
/// new subscribers can be added without re-introducing MediatR.
/// </summary>
public interface IDomainEventHandler<in TEvent> where TEvent : DomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
}
