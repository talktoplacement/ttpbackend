using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.Assessments.Domain;

/// <summary>
/// One input/expected-output pair for a coding question.
///
/// <see cref="IsSample"/> is the security boundary of the grader: sample cases are visible to the
/// student and are the only ones the interactive "Run" action may execute, while the remainder stay
/// hidden and decide the final score. Without that split a student could iterate against the full
/// grader until everything passed.
/// </summary>
public sealed class AssessmentTestCase : Entity<int>
{
    [Required] public int QuestionId { get; set; }

    public int OrderIndex { get; set; }

    /// <summary>Text fed to the program on stdin.</summary>
    public string Input { get; set; } = string.Empty;

    /// <summary>Expected stdout. Compared after trimming trailing whitespace per line.</summary>
    public string ExpectedOutput { get; set; } = string.Empty;

    /// <summary>Visible to the student and runnable before submission.</summary>
    public bool IsSample { get; set; }

    /// <summary>
    /// Relative weight when apportioning the question's marks across its cases, so a harder case can
    /// count for more than a trivial one.
    /// </summary>
    public int Weight { get; set; } = 1;

    [ForeignKey(nameof(QuestionId))]
    public AssessmentQuestion? Question { get; set; }
}
