using CareerPlatform.Api.Features.PlacementCompanies.Domain;

namespace CareerPlatform.Api.Features.PlacementCompanies.Dto;

/// <summary>Matches the frontend `CompanyProfile` shape verbatim.</summary>
public sealed record PlacementCompanyResponse(
    string Id, string Slug, string Name, string? Logo,
    string Tier, string CtcRange, IReadOnlyList<string> HiringRoles,
    int OpenPositions, string Description, bool IsPublished)
{
    public static PlacementCompanyResponse From(PlacementCompany c)
    {
        ArgumentNullException.ThrowIfNull(c);
        var roles = string.IsNullOrWhiteSpace(c.HiringRoles)
            ? Array.Empty<string>()
            : c.HiringRoles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return new PlacementCompanyResponse(
            c.Id.ToString(), c.Slug, c.Name,
            string.IsNullOrWhiteSpace(c.Logo) ? null : c.Logo,
            c.Tier, c.CtcRange, roles, c.OpenPositions, c.Description, c.IsPublished);
    }
}

/// <summary>Body for <c>POST /api/v1/admin/placement-companies</c>.</summary>
public sealed record CreatePlacementCompanyRequest(
    string Slug, string Name, string? Logo, string Tier, string? CtcRange,
    IReadOnlyList<string>? HiringRoles, int OpenPositions,
    string? Description, bool IsPublished);

/// <summary>Body for <c>PUT /api/v1/admin/placement-companies/{id}</c>. Every field optional.</summary>
public sealed record UpdatePlacementCompanyRequest(
    string? Slug, string? Name, string? Logo, string? Tier, string? CtcRange,
    IReadOnlyList<string>? HiringRoles, int? OpenPositions,
    string? Description, bool? IsPublished);
