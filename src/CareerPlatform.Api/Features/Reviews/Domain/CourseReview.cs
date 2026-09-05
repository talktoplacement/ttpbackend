using System.ComponentModel.DataAnnotations;
using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.Reviews.Domain;

/// <summary>
/// A student review left on a course. Rendered on the public course-detail page once moderated.
/// Moderation lifecycle: <c>pending</c> → <c>approved</c> | <c>rejected</c>. Approved reviews are
/// the only ones ever displayed publicly.
/// </summary>
public sealed class CourseReview : AuditableEntity<int>
{
    [Required, MaxLength(64)] public string UserId { get; set; } = string.Empty;

    public int CourseId { get; set; }

    /// <summary>1..5.</summary>
    public int Rating { get; set; }

    [Required, MaxLength(2000)] public string Comment { get; set; } = string.Empty;

    /// <summary><c>pending</c> / <c>approved</c> / <c>rejected</c>.</summary>
    [Required, MaxLength(16)] public string Status { get; set; } = "pending";

    /// <summary>UserId of the admin who last moderated. Null while pending.</summary>
    [MaxLength(64)] public string? ModeratedBy { get; set; }

    public DateTime? ModeratedAtUtc { get; set; }
}
