using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.SubscriptionPlans.Domain.Events;

/// <summary>
/// Raised when a <see cref="Subscription"/> becomes active for a student (Req 9.6). Dispatched by the
/// existing domain-event interceptor after the provisioning commit, so entitlement/notification
/// side-effects stay decoupled from the verify handler.
/// </summary>
/// <remarks>
/// Lives in a nested <c>Domain.Events</c> namespace rather than the exact <c>Domain</c> namespace:
/// domain events are not entities, and the domain-base-type architecture fitness test requires every
/// concrete class directly under a <c>Features.&lt;Feature&gt;.Domain</c> namespace to derive from
/// <c>Entity&lt;&gt;</c>. Keeping the event under <c>Domain/Events/</c> preserves the design intent of
/// co-locating it with the aggregate while keeping that convention test green.
/// </remarks>
/// <param name="EventId">A unique identifier for this event instance.</param>
/// <param name="OccurredOnUtc">The UTC time at which the subscription was activated.</param>
/// <param name="StudentId">The student the subscription was provisioned for.</param>
/// <param name="PlanId">The purchased plan's identifier.</param>
/// <param name="PlanName">The purchased plan's display name.</param>
public sealed record SubscriptionActivatedDomainEvent(
    Guid EventId, DateTime OccurredOnUtc, string StudentId, int PlanId, string PlanName)
    : DomainEvent(EventId, OccurredOnUtc);
