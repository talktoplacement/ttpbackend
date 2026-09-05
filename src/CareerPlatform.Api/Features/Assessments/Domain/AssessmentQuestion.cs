using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.Assessments.Domain;

/// <summary>The kinds of question an assessment can contain.</summary>
public static class AssessmentQuestionType
{
    /// <summary>Single-answer multiple choice, graded by index comparison.</summary>
    public const string MultipleChoice = "mcq";

    /// <summary>Free-form code, graded by executing hidden test cases.</summary>
    public const string Coding = "coding";

    public static bool IsSupported(string? value) =>
        value is MultipleChoice or Coding;
}

/// <summary>
/// One structured question inside an <see cref="Assessment"/>.
///
/// Replaces the opaque <c>Assessment.QuestionsJson</c> blob for anything gradable: hidden test cases
/// and correct answers must live in queryable columns so the SERVER can score an attempt. Previously
/// the client submitted its own score, which meant any student could claim a perfect result.
/// </summary>
public sealed class AssessmentQuestion : AuditableEntity<int>
{
    [Required] public int AssessmentId { get; set; }

    /// <summary>Display position within the assessment.</summary>
    public int OrderIndex { get; set; }

    /// <summary>One of <see cref="AssessmentQuestionType"/>.</summary>
    [Required, MaxLength(16)]
    public string QuestionType { get; set; } = AssessmentQuestionType.MultipleChoice;

    [Required, MaxLength(300)] public string Title { get; set; } = string.Empty;

    /// <summary>Problem statement, rendered as markdown by the client.</summary>
    public string PromptMarkdown { get; set; } = string.Empty;

    /// <summary>Marks awarded for a fully-correct answer.</summary>
    public int Marks { get; set; } = 1;

    // ── Multiple choice ──────────────────────────────────────────────────────

    /// <summary>JSON array of option strings.</summary>
    public string? OptionsJson { get; set; }

    /// <summary>
    /// Zero-based index of the correct option. NEVER projected into a student-facing response —
    /// only the grader reads it.
    /// </summary>
    public int? CorrectOptionIndex { get; set; }

    // ── Coding ───────────────────────────────────────────────────────────────

    /// <summary>Name of the function the student is expected to implement.</summary>
    [MaxLength(128)] public string? FunctionName { get; set; }

    /// <summary>Per-language starter code as a JSON object keyed by language id.</summary>
    public string? StarterCodeJson { get; set; }

    /// <summary>Per-execution wall-clock budget passed to the code executor.</summary>
    public int TimeLimitMs { get; set; } = 5000;

    [ForeignKey(nameof(AssessmentId))]
    public Assessment? Assessment { get; set; }

    public ICollection<AssessmentTestCase> TestCases { get; set; } = new List<AssessmentTestCase>();

    public bool IsCoding => string.Equals(
        QuestionType, AssessmentQuestionType.Coding, StringComparison.OrdinalIgnoreCase);
}
