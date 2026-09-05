using CareerPlatform.Api.Features.SubscriptionPlans.Domain;

namespace CareerPlatform.Api.Features.SubscriptionPlans.Dto;

/// <summary>Admin projection (all fields).</summary>
public sealed record PlanResponse(
    int Id, string Code, string Name, string Description,
    decimal Price, string Currency, BillingPeriodUnit IntervalUnit, int IntervalCount, bool IsActive)
{
    public static PlanResponse From(SubscriptionPlan p)
    {
        ArgumentNullException.ThrowIfNull(p);
        return new PlanResponse(p.Id, p.Code, p.Name, p.Description, p.Price, p.Currency,
            p.IntervalUnit, p.IntervalCount, p.IsActive);
    }
}

/// <summary>Student catalog projection (excludes IsActive because every catalog row is active).</summary>
public sealed record CatalogPlanResponse(
    int Id, string Code, string Name, string Description,
    decimal Price, string Currency, BillingPeriodUnit IntervalUnit, int IntervalCount);

public sealed record EntitlementResponse(string EffectivePlan);

public sealed record CreatePlanRequest(
    string Code, string Name, string? Description, decimal Price, string? Currency,
    BillingPeriodUnit IntervalUnit, int IntervalCount, bool IsActive);

public sealed record UpdatePlanRequest(
    string? Code, string? Name, string? Description, decimal? Price, string? Currency,
    BillingPeriodUnit? IntervalUnit, int? IntervalCount);

public sealed record SetPlanActiveRequest(bool IsActive);
