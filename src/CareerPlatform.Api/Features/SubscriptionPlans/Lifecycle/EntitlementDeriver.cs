using CareerPlatform.Api.Features.SubscriptionPlans.Domain;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.SubscriptionPlans.Lifecycle;

/// <summary>
/// The single, authoritative derivation of a student's effective plan (Req 11.4, 12.1, 12.2, 12.3).
/// The derived value is computed from the student's <em>current active subscription</em> — the
/// subscription whose <see cref="SubscriptionStatus"/> is <see cref="SubscriptionStatus.Active"/>
/// and whose period contains <c>now</c> — and takes precedence over the denormalized
/// <see cref="Features.Users.Domain.UserProfile.PlanName"/> cache. When no such subscription exists,
/// the effective plan is <c>"Free"</c>.
///
/// Exposed as a pure, parameterized helper so both the entitlement query handler and the expiry
/// sweeper apply exactly the same rule, and so property tests can drive it deterministically at a
/// fixed <c>now</c>.
/// </summary>
public static class EntitlementDeriver
{
    /// <summary>The effective plan reported when a student has no active subscription (Req 12.2).</summary>
    public const string FreePlan = "Free";

    /// <summary>
    /// The single authoritative test for whether a plan grants paid ("pro") entitlements.
    ///
    /// Defined as "anything that is not the free plan" rather than matching a hardcoded list of
    /// plan-name substrings. That keeps entitlement fully data-driven: an operator can add a new
    /// paid tier (e.g. "Elite") through the admin catalog and it unlocks paid features immediately,
    /// with no code change and no risk of a rename silently downgrading users.
    /// </summary>
    public static bool IsProPlan(string? planName) =>
        !string.IsNullOrWhiteSpace(planName)
        && !planName.Trim().Equals(FreePlan, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Derives the student's effective plan at <paramref name="nowUtc"/>: the plan name of the most
    /// recent subscription that is active at that instant (Req 12.1), else <c>"Free"</c> (Req 12.2).
    /// </summary>
    public static async Task<string> DeriveEffectivePlanAsync(
        AppDbContext db, string studentId, DateTime nowUtc, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(db);

        var activePlanName = await db.Subscriptions
            .Where(s => s.StudentId == studentId
                     && s.Status == SubscriptionStatus.Active
                     && s.StartDate <= nowUtc
                     && nowUtc < s.EndDate)
            .OrderByDescending(s => s.StartDate)
            .Select(s => s.Plan!.Name)
            .FirstOrDefaultAsync(ct);

        return activePlanName ?? FreePlan;
    }
}
