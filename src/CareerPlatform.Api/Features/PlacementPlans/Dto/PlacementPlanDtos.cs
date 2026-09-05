using CareerPlatform.Api.Features.PlacementPlans.Domain;

namespace CareerPlatform.Api.Features.PlacementPlans.Dto;

public sealed record PlacementPlanResponse(
    int Id, string Slug, string Title, string Description,
    int DurationWeeks, decimal Price, bool IsPublished)
{
    public static PlacementPlanResponse From(PlacementPlan p)
    {
        ArgumentNullException.ThrowIfNull(p);
        return new PlacementPlanResponse(
            p.Id, p.Slug, p.Title, p.Description, p.DurationWeeks, p.Price, p.IsPublished);
    }
}

/// <summary>Body for <c>POST /api/v1/admin/placement-plans</c>.</summary>
public sealed record CreatePlacementPlanRequest(
    string Slug, string Title, string? Description,
    int DurationWeeks, decimal Price, bool IsPublished = true);

/// <summary>Body for <c>PUT /api/v1/admin/placement-plans/{id}</c>. Every field optional.</summary>
public sealed record UpdatePlacementPlanRequest(
    string? Slug, string? Title, string? Description,
    int? DurationWeeks, decimal? Price, bool? IsPublished);
