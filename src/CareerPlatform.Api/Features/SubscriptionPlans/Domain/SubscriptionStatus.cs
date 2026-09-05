namespace CareerPlatform.Api.Features.SubscriptionPlans.Domain;

/// <summary>
/// Lifecycle state of a provisioned <see cref="Subscription"/> (Req 9.3, 9.7, 11.1, 11.2). A
/// subscription is entitlement-bearing only while <see cref="Active"/> and within its period.
/// </summary>
public enum SubscriptionStatus
{
    Pending = 0,
    Active = 1,
    Expired = 2,
    Superseded = 3,
}
