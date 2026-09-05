using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.SubscriptionPlans.Domain;

/// <summary>
/// A database-backed, admin-managed subscription catalog entry (Req 1). It is the single source of
/// truth for subscription pricing: the order amount, currency, and billing period are all read from
/// here at request time, replacing the former hardcoded price constants (Req 14.7).
/// </summary>
public sealed class SubscriptionPlan : AuditableEntity<int>
{
    /// <summary>Unique, stable code, e.g. <c>"monthly-pro"</c> (Req 1.1).</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Display name, e.g. <c>"Monthly (Pro)"</c> (Req 1.1).</summary>
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>Non-negative price in whole INR rupees (Req 1.4).</summary>
    public decimal Price { get; set; }

    /// <summary>Currency; defaults to INR (Req 1.5).</summary>
    public string Currency { get; set; } = "INR";

    /// <summary>The calendar unit of the billing period (Req 1.2).</summary>
    public BillingPeriodUnit IntervalUnit { get; set; } = BillingPeriodUnit.Month;

    /// <summary>The number of <see cref="IntervalUnit"/> units in the billing period (Req 1.2).</summary>
    public int IntervalCount { get; set; } = 1;

    /// <summary>Whether the plan is published/purchasable (Req 4).</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Computes the end date for a subscription starting at <paramref name="startUtc"/> using this
    /// plan's billing period (Req 1.2, 9.2).
    /// </summary>
    public DateTime ComputeEndDate(DateTime startUtc) =>
        BillingPeriodCalculator.ComputeEndDate(startUtc, IntervalUnit, IntervalCount);
}
