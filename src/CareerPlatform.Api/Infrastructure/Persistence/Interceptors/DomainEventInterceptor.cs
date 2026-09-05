using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CareerPlatform.Api.Infrastructure.Persistence.Interceptors;

/// <summary>
/// SaveChanges interceptor that dispatches domain events <em>after</em> a successful commit (Req 11).
/// EF Core invokes <c>SavedChanges</c>/<c>SavedChangesAsync</c> only when the save committed, so a
/// failed commit dispatches nothing and leaves events on their aggregates (Req 11.2). Events are
/// collected and cleared from their aggregates <em>before</em> dispatch, so an immediately following
/// save dispatches zero previously-dispatched events (Req 11.3). Each collected event is dispatched
/// exactly once (Req 11.1); if a handler throws, the failing event is surfaced via
/// <see cref="DomainEventDispatchException"/> without rolling back the committed transaction and
/// without re-dispatching already-dispatched events (Req 11.4).
/// </summary>
public sealed class DomainEventInterceptor : SaveChangesInterceptor
{
    private readonly IDomainEventDispatcher _dispatcher;

    public DomainEventInterceptor(IDomainEventDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    /// <inheritdoc />
    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        DispatchDomainEventsAsync(eventData.Context, CancellationToken.None).GetAwaiter().GetResult();
        return base.SavedChanges(eventData, result);
    }

    /// <inheritdoc />
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await DispatchDomainEventsAsync(eventData.Context, cancellationToken);
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private async Task DispatchDomainEventsAsync(DbContext? context, CancellationToken cancellationToken)
    {
        if (context is null)
        {
            return;
        }

        var aggregates = context.ChangeTracker.Entries<IHasDomainEvents>()
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.DomainEvents.Count > 0)
            .ToList();

        if (aggregates.Count == 0)
        {
            return;
        }

        // Snapshot the events, then clear them from their aggregates BEFORE dispatching so a
        // subsequent save on the same aggregates dispatches zero previously-dispatched events (Req 11.3).
        var domainEvents = aggregates.SelectMany(aggregate => aggregate.DomainEvents).ToList();
        foreach (var aggregate in aggregates)
        {
            aggregate.ClearDomainEvents();
        }

        // Dispatch each event exactly once (Req 11.1). A handler failure surfaces which event failed
        // without rolling back the committed transaction or re-dispatching earlier events (Req 11.4).
        foreach (var domainEvent in domainEvents)
        {
            try
            {
                await _dispatcher.Dispatch(domainEvent, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new DomainEventDispatchException(domainEvent, ex);
            }
        }
    }
}
