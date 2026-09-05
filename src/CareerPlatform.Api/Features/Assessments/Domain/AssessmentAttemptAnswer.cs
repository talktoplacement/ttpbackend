using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.Assessments.Domain;

/// <summary>
/// A student's answer to one question within one attempt.
///
/// Draft saves upsert this row (unique on attempt+question), so a browser crash or lost connection
/// never costs the student their work. The grading columns are written exclusively by the server-side
/// grader at submit time — nothing the client sends can influence them.
/// </summary>
public sealed class AssessmentAttemptAnswer : AuditableEntity<int>
{
    [Required] public int AttemptId { get; set; }
    [Required] public int QuestionId { get; set; }

    /// <summary>Chosen option for a multiple-choice question.</summary>
    public int? SelectedOptionIndex { get; set; }

    /// <summary>Language id for a coding answer (matches the executor's language catalog).</summary>
    [MaxLength(32)] public string? Language { get; set; }

    /// <summary>Submitted source code for a coding answer.</summary>
    public string? SourceCode { get; set; }

    // ── Grading output (server-written only) ─────────────────────────────────

    public int AwardedMarks { get; set; }

    /// <summary>True when the answer earned full marks; null until graded.</summary>
    public bool? IsCorrect { get; set; }

    public int PassedTestCount { get; set; }
    public int TotalTestCount { get; set; }
    public DateTime? EvaluatedAtUtc { get; set; }

    [ForeignKey(nameof(AttemptId))]
    public AssessmentAttempt? Attempt { get; set; }

    [ForeignKey(nameof(QuestionId))]
    public AssessmentQuestion? Question { get; set; }
}
