namespace CareerPlatform.Api.Features.SubscriptionPlans.Domain;

/// <summary>
/// Pure, side-effect-free end-date computation. THE single place that turns a plan's stored
/// (unit, count) period into an <c>EndDate</c> from a <c>StartDate</c> (Req 1.2, 9.2). Calendar-aware
/// so "1 Month" means <c>AddMonths(1)</c>, not 30 days.
/// </summary>
public static class BillingPeriodCalculator
{
    /// <summary>
    /// Computes the subscription end date by adding <paramref name="count"/> units of
    /// <paramref name="unit"/> to <paramref name="startUtc"/>. Guards <paramref name="count"/> to be
    /// at least 1 so the end date is always strictly after the start (Req 1.2, 9.2).
    /// </summary>
    public static DateTime ComputeEndDate(DateTime startUtc, BillingPeriodUnit unit, int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        return unit switch
        {
            BillingPeriodUnit.Day => startUtc.AddDays(count),
            BillingPeriodUnit.Week => startUtc.AddDays(7 * count),
            BillingPeriodUnit.Month => startUtc.AddMonths(count),
            BillingPeriodUnit.Year => startUtc.AddYears(count),
            _ => throw new ArgumentOutOfRangeException(nameof(unit)),
        };
    }
}
