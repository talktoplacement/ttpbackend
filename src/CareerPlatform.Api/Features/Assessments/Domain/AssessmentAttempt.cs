using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.Assessments.Domain;

/// <summary>
/// One row per student attempt at an <see cref="Assessment"/>. Answers are stored as JSON so the
/// question shape on the parent assessment can evolve in code without further schema changes.
/// <c>Score</c>, <c>TimeTakenMinutes</c>, and <c>Passed</c> remain <c>null</c> until submit —
/// the same row is reused across start → submit so the student can resume an in-progress
/// attempt.
/// </summary>
public sealed class AssessmentAttempt : AuditableEntity<int>
{
    [Required, MaxLength(64)] public string UserId { get; set; } = string.Empty;

    [Required] public int AssessmentId { get; set; }

    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAtUtc { get; set; }

    public int? Score { get; set; }

    /// <summary>Snapshot of <see cref="Assessment.TotalMarks"/> at start time.</summary>
    public int TotalMarks { get; set; }

    /// <summary>Snapshot of <see cref="Assessment.PassingMarks"/> at start time.</summary>
    public int PassingMarks { get; set; }

    /// <summary>Optional percentile rank against other attempts; populated by an offline job.</summary>
    [Column(TypeName = "numeric(5, 2)")]
    public decimal? Percentile { get; set; }

    public int? TimeTakenMinutes { get; set; }

    public bool? Passed { get; set; }

    /// <summary>Raw answer payload as JSON. Schema is up to the client + assessment.</summary>
    [Required]
    public string AnswersJson { get; set; } = "{}";

    [ForeignKey(nameof(AssessmentId))]
    public Assessment? Assessment { get; set; }
}
