namespace CareerPlatform.Api.Features.Broadcasts.Domain;

/// <summary>
/// Distinguishes the two admin broadcast surfaces backed by this feature: an in-app
/// <see cref="Notification"/> feed entry (bell icon), or a <see cref="Promotion"/> email campaign.
/// Persisted as an int so adding a new type later is data-compatible.
/// </summary>
public enum BroadcastType
{
    Notification = 0,
    Promotion = 1,
}
