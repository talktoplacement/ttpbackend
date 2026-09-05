using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.Broadcasts.Service;

/// <summary>
/// A single broadcast recipient, projected straight out of the database so the send path never has
/// to re-query for addresses.
/// </summary>
/// <param name="UserId">Owner of the fanned-out in-app notification row.</param>
/// <param name="Email">Delivery address for <c>Promotion</c> broadcasts; may be blank.</param>
internal sealed record BroadcastRecipient(string UserId, string Email);

/// <summary>
/// Turns an admin-selected target-plan label into the concrete audience a broadcast should reach.
/// Kept in one place so the recipient-count endpoint and the send endpoint compute the identical
/// audience — a preview that disagrees with the actual fan-out is worse than no preview.
/// </summary>
internal static class AudienceResolver
{
    /// <summary>
    /// Sentinel target meaning "every student regardless of plan". Compared case-insensitively so
    /// the client is not coupled to this exact casing.
    /// </summary>
    public const string AllPlans = "All Plans";

    /// <summary>
    /// Role that broadcasts address. Admin accounts are deliberately excluded: a promotional
    /// campaign or problem-of-the-day must not land in staff inboxes and must not inflate the
    /// recipient count the admin is shown before sending.
    /// </summary>
    public const string StudentRole = "Student";

    /// <summary>Normalises a caller-supplied target, collapsing null/blank onto <see cref="AllPlans"/>.</summary>
    public static string NormalizeTarget(string? targetPlan)
        => string.IsNullOrWhiteSpace(targetPlan) ? AllPlans : targetPlan.Trim();

    public static IQueryable<BroadcastRecipient> Resolve(AppDbContext db, string? targetPlan)
    {
        ArgumentNullException.ThrowIfNull(db);

        var query = db.UserProfiles.AsNoTracking().Where(u => u.Role == StudentRole);

        var target = NormalizeTarget(targetPlan);
        if (!string.Equals(target, AllPlans, StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(u => u.PlanName == target);
        }

        return query.Select(u => new BroadcastRecipient(u.Id, u.Email));
    }

    public static Task<int> CountAsync(AppDbContext db, string? targetPlan, CancellationToken ct)
        => Resolve(db, targetPlan).CountAsync(ct);
}
