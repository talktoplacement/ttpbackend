using System.ComponentModel.DataAnnotations;
using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.LearningPaths.Domain;

/// <summary>
/// Curated learning path shown on the public learning-paths pages. Milestones are stored as JSON
/// so the schema stays flat while the domain can evolve. All content is admin-managed via the
/// CRUD endpoints — nothing hardcoded on the frontend.
/// </summary>
public sealed class LearningPath : AuditableEntity<int>
{
    [Required, MaxLength(160)] public string Slug { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string Title { get; set; } = string.Empty;
    [MaxLength(4000)] public string Description { get; set; } = string.Empty;
    [Required, MaxLength(120)] public string TargetRole { get; set; } = string.Empty;
    public int EstimatedMonths { get; set; }
    /// <summary>JSON-serialized array of milestone objects. Never hand-parsed — go via the contract.</summary>
    public string MilestonesJson { get; set; } = "[]";
    public bool IsPopular { get; set; }
    public bool IsPublished { get; set; } = true;
}
