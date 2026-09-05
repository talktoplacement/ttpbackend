using System.ComponentModel.DataAnnotations;
using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.PlacementRoles.Domain;

/// <summary>
/// Target engineering role a student is preparing for (SDE-1, Frontend, Backend, …). Rendered on
/// the public /placement/roles page and paired with placement companies for hiring criteria.
/// </summary>
public sealed class PlacementRole : AuditableEntity<int>
{
    [Required, MaxLength(64)] public string Slug { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string Title { get; set; } = string.Empty;
    [MaxLength(64)] public string? AvgCtcRange { get; set; }
    [MaxLength(4000)] public string? RequirementsMarkdown { get; set; }
    public bool IsPublished { get; set; } = true;
}
