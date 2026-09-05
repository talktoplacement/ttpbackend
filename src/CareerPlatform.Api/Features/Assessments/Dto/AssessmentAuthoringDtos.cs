using CareerPlatform.Api.Features.Assessments.Domain;

namespace CareerPlatform.Api.Features.Assessments.Dto;

/// <summary>
/// One authored test case. <c>IsSample</c> decides whether the student can see and run it, so it is
/// the single knob that separates worked examples from the hidden grading set.
/// </summary>
public sealed record AuthoredTestCase(
    string Input,
    string ExpectedOutput,
    bool IsSample,
    int Weight);

/// <summary>
/// One authored question, including the answer key. Admin-only in both directions — this record is
/// never returned on a student-facing route.
/// </summary>
public sealed record AuthoredQuestion(
    string QuestionType,
    string Title,
    string? PromptMarkdown,
    int Marks,
    IReadOnlyList<string>? Options,
    int? CorrectOptionIndex,
    string? FunctionName,
    IReadOnlyDictionary<string, string>? StarterCode,
    int? TimeLimitMs,
    IReadOnlyList<AuthoredTestCase>? TestCases);

/// <summary>
/// The complete question bank for an assessment.
///
/// Deliberately a whole-bank replace rather than per-question CRUD: a bank is edited as a unit, and
/// replacing it atomically keeps <c>OrderIndex</c>, <c>QuestionsCount</c>, and <c>TotalMarks</c>
/// consistent, which incremental edits repeatedly got wrong.
/// </summary>
public sealed record ReplaceQuestionBankRequest(IReadOnlyList<AuthoredQuestion> Questions);

/// <summary>Admin view of an authored question, answer key included.</summary>
public sealed record AuthoredQuestionResponse(
    int Id,
    int OrderIndex,
    string QuestionType,
    string Title,
    string PromptMarkdown,
    int Marks,
    IReadOnlyList<string> Options,
    int? CorrectOptionIndex,
    string? FunctionName,
    IReadOnlyDictionary<string, string> StarterCode,
    int TimeLimitMs,
    IReadOnlyList<AuthoredTestCaseResponse> TestCases);

/// <summary>Admin view of a test case, hidden cases included.</summary>
public sealed record AuthoredTestCaseResponse(
    int Id, int OrderIndex, string Input, string ExpectedOutput, bool IsSample, int Weight);

/// <summary>The question bank plus the totals derived from it.</summary>
public sealed record QuestionBankResponse(
    int AssessmentId,
    int QuestionsCount,
    int TotalMarks,
    IReadOnlyList<AuthoredQuestionResponse> Questions)
{
    public static QuestionBankResponse From(int assessmentId, IReadOnlyList<AssessmentQuestion> questions)
    {
        ArgumentNullException.ThrowIfNull(questions);
        var items = questions
            .OrderBy(q => q.OrderIndex).ThenBy(q => q.Id)
            .Select(q => new AuthoredQuestionResponse(
                q.Id, q.OrderIndex, q.QuestionType, q.Title, q.PromptMarkdown, q.Marks,
                JsonPayload.ReadStringArray(q.OptionsJson),
                q.CorrectOptionIndex,
                q.FunctionName,
                JsonPayload.ReadStringMap(q.StarterCodeJson),
                q.TimeLimitMs,
                (q.TestCases ?? new List<AssessmentTestCase>())
                    .OrderBy(t => t.OrderIndex)
                    .Select(t => new AuthoredTestCaseResponse(
                        t.Id, t.OrderIndex, t.Input, t.ExpectedOutput, t.IsSample, t.Weight))
                    .ToList()))
            .ToList();

        return new QuestionBankResponse(
            assessmentId, items.Count, questions.Sum(q => q.Marks), items);
    }
}
