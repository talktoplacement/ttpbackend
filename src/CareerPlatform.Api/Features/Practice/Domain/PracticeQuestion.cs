using System.ComponentModel.DataAnnotations;
using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.Practice.Domain;

/// <summary>
/// A coding-practice question rendered on the public /practice pages. All content — title, body,
/// difficulty, category, company tags — is admin-managed via the CRUD endpoints below so the
/// student catalog contains no hardcoded questions.
/// </summary>
public sealed class PracticeQuestion : AuditableEntity<int>
{
    /// <summary>Unique URL-safe slug (natural key).</summary>
    [Required, MaxLength(160)]
    public string Slug { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>Markdown description of the question.</summary>
    [MaxLength(8000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Easy / Medium / Hard.</summary>
    [Required, MaxLength(16)]
    public string Difficulty { get; set; } = "Easy";

    [Required, MaxLength(64)]
    public string Category { get; set; } = string.Empty;

    /// <summary>0..100 acceptance rate percentage, admin-tracked.</summary>
    public int AcceptanceRate { get; set; }

    /// <summary>Comma-separated company tags (denormalized for read-simplicity).</summary>
    [MaxLength(500)]
    public string CompanyTags { get; set; } = string.Empty;

    /// <summary>Whether the question is publicly listed. Admins draft with false first.</summary>
    public bool IsPublished { get; set; } = true;
}
