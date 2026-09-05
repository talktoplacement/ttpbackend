using CareerPlatform.Api.Features.Assessments.Domain;

namespace CareerPlatform.Api.Features.Assessments.Dto;

/// <summary>
/// A question as shown to a student mid-attempt.
///
/// Deliberately omits <c>CorrectOptionIndex</c> and every non-sample test case: this record is the
/// projection boundary that stops the answer key from being served to the browser. Only sample test
/// cases are included, and only they may be executed by the interactive "Run" action.
/// </summary>
public sealed record RunnerQuestionResponse(
    int Id,
    int OrderIndex,
    string QuestionType,
    string Title,
    string PromptMarkdown,
    int Marks,
    IReadOnlyList<string> Options,
    string? FunctionName,
    IReadOnlyDictionary<string, string> StarterCode,
    int TimeLimitMs,
    IReadOnlyList<RunnerSampleTestResponse> SampleTests)
{
    public static RunnerQuestionResponse From(AssessmentQuestion q)
    {
        ArgumentNullException.ThrowIfNull(q);
        return new RunnerQuestionResponse(
            q.Id,
            q.OrderIndex,
            q.QuestionType,
            q.Title,
            q.PromptMarkdown,
            q.Marks,
            JsonPayload.ReadStringArray(q.OptionsJson),
            q.FunctionName,
            JsonPayload.ReadStringMap(q.StarterCodeJson),
            q.TimeLimitMs,
            (q.TestCases ?? new List<AssessmentTestCase>())
                .Where(t => t.IsSample)
                .OrderBy(t => t.OrderIndex)
                .Select(t => new RunnerSampleTestResponse(t.Id, t.Input, t.ExpectedOutput))
                .ToList());
    }
}

/// <summary>A visible example case for a coding question.</summary>
public sealed record RunnerSampleTestResponse(int Id, string Input, string ExpectedOutput);

/// <summary>The student's saved answer for one question, replayed when an attempt is resumed.</summary>
public sealed record RunnerSavedAnswerResponse(
    int QuestionId,
    int? SelectedOptionIndex,
    string? Language,
    string? SourceCode);

/// <summary>Everything the attempt runner needs to render and resume a live attempt.</summary>
public sealed record AttemptRunnerResponse(
    int AttemptId,
    int AssessmentId,
    string AssessmentSlug,
    string AssessmentTitle,
    int DurationMinutes,
    string StartedAt,
    string? SubmittedAt,
    /// <summary>Server-computed remaining seconds; the client must not derive the deadline itself.</summary>
    int RemainingSeconds,
    int TotalMarks,
    int PassingMarks,
    bool IsSubmitted,
    /// <summary>False when no sandbox is configured, so the UI can hide the Run action.</summary>
    bool CodeExecutionEnabled,
    IReadOnlyList<CodeLanguageResponse> Languages,
    IReadOnlyList<RunnerQuestionResponse> Questions,
    IReadOnlyList<RunnerSavedAnswerResponse> SavedAnswers);

/// <summary>A language the student may submit in.</summary>
public sealed record CodeLanguageResponse(string Id, string Label);

/// <summary>Body for saving one question's draft answer.</summary>
public sealed record SaveAnswerRequest(
    int QuestionId,
    int? SelectedOptionIndex,
    string? Language,
    string? SourceCode);

/// <summary>Body for running code against the visible sample cases only.</summary>
public sealed record RunCodeRequest(int QuestionId, string Language, string SourceCode);

/// <summary>Result of one sample case during an interactive run.</summary>
public sealed record SampleRunResult(
    int TestCaseId,
    string Input,
    string ExpectedOutput,
    string ActualOutput,
    bool Passed,
    bool TimedOut,
    string? Stderr);

/// <summary>Outcome of an interactive run across all sample cases.</summary>
public sealed record RunCodeResponse(
    bool ExecutionAvailable,
    string? FailureReason,
    int PassedCount,
    int TotalCount,
    IReadOnlyList<SampleRunResult> Results);

/// <summary>Per-question breakdown on the scorecard.</summary>
public sealed record QuestionScoreResponse(
    int QuestionId,
    string Title,
    string QuestionType,
    int Marks,
    int AwardedMarks,
    bool IsCorrect,
    int PassedTestCount,
    int TotalTestCount);

/// <summary>The graded result of a submitted attempt.</summary>
public sealed record AttemptScorecardResponse(
    int AttemptId,
    int Score,
    int TotalMarks,
    int PassingMarks,
    bool Passed,
    int TimeTakenMinutes,
    string SubmittedAt,
    IReadOnlyList<QuestionScoreResponse> Questions);
