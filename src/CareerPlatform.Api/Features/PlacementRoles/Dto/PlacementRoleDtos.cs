using CareerPlatform.Api.Features.PlacementRoles.Domain;

namespace CareerPlatform.Api.Features.PlacementRoles.Dto;

public sealed record PlacementRoleResponse(
    int Id, string Slug, string Title,
    string? AvgCtcRange, string? RequirementsMarkdown, bool IsPublished)
{
    public static PlacementRoleResponse From(PlacementRole r)
    {
        ArgumentNullException.ThrowIfNull(r);
        return new PlacementRoleResponse(
            r.Id, r.Slug, r.Title, r.AvgCtcRange, r.RequirementsMarkdown, r.IsPublished);
    }
}

public sealed record CreatePlacementRoleRequest(
    string Slug, string Title,
    string? AvgCtcRange, string? RequirementsMarkdown, bool IsPublished = true);

public sealed record UpdatePlacementRoleRequest(
    string Title, string? AvgCtcRange, string? RequirementsMarkdown, bool IsPublished);
