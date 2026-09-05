using System.ComponentModel.DataAnnotations;
using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.Skills.Domain;

/// <summary>
/// A skill the authenticated user claims on their profile. One row per (user, skill).
/// The skill catalog is intentionally free-form (no shared enum) — a skill matrix that only
/// lets users pick from a fixed list would be far more limiting than the value it adds today.
/// </summary>
public sealed class UserSkill : AuditableEntity<int>
{
    [Required, MaxLength(64)] public string UserId { get; set; } = string.Empty;

    [Required, MaxLength(100)] public string SkillName { get; set; } = string.Empty;

    [Required, MaxLength(64)] public string Category { get; set; } = string.Empty;

    /// <summary><c>Beginner</c> / <c>Intermediate</c> / <c>Advanced</c> / <c>Expert</c>.</summary>
    [Required, MaxLength(16)] public string ProficiencyLevel { get; set; } = "Intermediate";

    public int DisplayOrder { get; set; }
}
