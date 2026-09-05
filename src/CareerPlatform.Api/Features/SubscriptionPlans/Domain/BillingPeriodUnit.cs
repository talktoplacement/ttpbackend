namespace CareerPlatform.Api.Features.SubscriptionPlans.Domain;

/// <summary>
/// The calendar unit of a subscription plan's billing period. Combined with an interval count it
/// expresses any period as data only (Monthly = <c>(Month, 1)</c>, Yearly = <c>(Month, 12)</c>,
/// Quarterly = <c>(Month, 3)</c>, Weekly = <c>(Week, 1)</c>), so adding a new duration never
/// requires a code change (Req 1.2).
/// </summary>
public enum BillingPeriodUnit
{
    Day = 0,
    Week = 1,
    Month = 2,
    Year = 3,
}
