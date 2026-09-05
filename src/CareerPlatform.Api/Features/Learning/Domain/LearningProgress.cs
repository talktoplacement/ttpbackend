using System.ComponentModel.DataAnnotations;
using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.Learning.Domain;

/// <summary>
/// A single learning-progress row for a (user, resource) pair. Polymorphic: the same table
/// backs progress on Courses, LearningPaths, and individual Topics. The unique index on
/// <c>(UserId, ResourceType, ResourceId)</c> keeps the upsert endpoint idempotent.
///
/// <see cref="ResourceType"/> is a free-text discriminator validated at the API boundary
/// (allowed values: <c>Course</c>, <c>LearningPath</c>, <c>Topic</c>). Referential integrity to
/// the target row is enforced at the API layer, not the DB — that keeps this table decoupled
/// from every downstream feature.
/// </summary>
public sealed class LearningProgress : AuditableEntity<int>
{
    [Required, MaxLength(64)] public string UserId { get; set; } = string.Empty;

    [Required, MaxLength(32)] public string ResourceType { get; set; } = string.Empty;

    [Required] public int ResourceId { get; set; }

    /// <summary>Coarse status: <c>not-started</c> / <c>in-progress</c> / <c>completed</c>.</summary>
    [Required, MaxLength(16)] public string Status { get; set; } = "not-started";

    /// <summary>Percent complete 0..100. Handler clamps out-of-range values.</summary>
    public int PercentComplete { get; set; }

    public DateTime LastAccessedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Set once when <see cref="PercentComplete"/> hits 100 or <see cref="Status"/> is completed.</summary>
    public DateTime? CompletedAtUtc { get; set; }

    /// <summary>Free-text user note (bookmark position, personal reminder, etc.).</summary>
    [MaxLength(2000)] public string? Notes { get; set; }
}
