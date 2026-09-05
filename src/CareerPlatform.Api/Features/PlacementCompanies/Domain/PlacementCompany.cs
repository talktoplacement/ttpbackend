using System.ComponentModel.DataAnnotations;
using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.PlacementCompanies.Domain;

/// <summary>
/// A partner company shown on the public placement pages. All content is admin-managed via the
/// CRUD endpoints so the student-facing catalog contains no hardcoded rows.
/// </summary>
public sealed class PlacementCompany : AuditableEntity<int>
{
    [Required, MaxLength(160)]
    public string Slug { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Logo { get; set; } = string.Empty;

    /// <summary>Tier 1 / Product Based / Service Based / High Growth Startup.</summary>
    [Required, MaxLength(64)]
    public string Tier { get; set; } = "Product Based";

    [MaxLength(64)]
    public string CtcRange { get; set; } = string.Empty;

    /// <summary>Comma-separated hiring roles.</summary>
    [MaxLength(500)]
    public string HiringRoles { get; set; } = string.Empty;

    public int OpenPositions { get; set; }

    [MaxLength(4000)]
    public string Description { get; set; } = string.Empty;

    public bool IsPublished { get; set; } = true;
}
