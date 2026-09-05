using CareerPlatform.Api.Features.SubscriptionPlans.Lifecycle;
using CareerPlatform.Api.Features.Users.Domain;

namespace CareerPlatform.Api.Features.Users.Dto;

/// <summary>
/// Outward projection of the authenticated principal's profile.
///
/// <paramref name="EffectivePlan"/> and <paramref name="IsPro"/> are the server's authoritative
/// entitlement answer, derived from the user's current active subscription. Clients MUST gate paid
/// features on <paramref name="IsPro"/> rather than pattern-matching a plan label locally, so
/// entitlement can never be spoofed from the browser or drift when plans are renamed.
/// </summary>
public sealed record MyProfileResponse(
    string Id, string Email, string FullName, string Role, string PlanName,
    string? Phone, string? Designation, string? Department, DateTime CreatedAt,
    string EffectivePlan, bool IsPro)
{
    /// <summary>
    /// Maps the entity to its projection. <paramref name="effectivePlan"/> is the subscription-
    /// derived plan; when omitted the denormalized <c>PlanName</c> cache is used as the fallback.
    /// </summary>
    public static MyProfileResponse From(UserProfile profile, string? effectivePlan = null)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var plan = string.IsNullOrWhiteSpace(effectivePlan) ? profile.PlanName : effectivePlan;
        return new MyProfileResponse(
            profile.Id, profile.Email, profile.FullName, profile.Role, profile.PlanName,
            profile.Phone, profile.Designation, profile.Department, profile.CreatedAt,
            plan, EntitlementDeriver.IsProPlan(plan));
    }
}

public sealed record UpdateMyProfileRequest(
    string FullName, string? Phone, string? Designation, string? Department);

public sealed record ChangeMyPasswordRequest(string CurrentPassword, string NewPassword);

public sealed record SyncMyProfileRequest(string? DisplayName);
