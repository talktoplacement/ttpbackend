using CareerPlatform.Api.Features.Assessments.Dto;

namespace CareerPlatform.Api.Features.Assessments.Service;

/// <summary>
/// Admin authoring of an assessment's question bank.
///
/// Separate from <see cref="IAssessmentService"/> (assessment metadata) and
/// <see cref="IAssessmentRunnerService"/> (the exam runtime) because it is the only surface allowed to
/// read or write the answer key. Keeping it on its own interface makes the privilege boundary explicit
/// rather than relying on every caller of a general-purpose service to remember it.
/// </summary>
public interface IAssessmentAuthoringService
{
    /// <summary>Returns the full bank including correct options and hidden test cases.</summary>
    Task<Result<QuestionBankResponse>> GetBankAsync(int assessmentId, CancellationToken ct);

    /// <summary>
    /// Atomically replaces the bank and re-derives the assessment's <c>QuestionsCount</c> and
    /// <c>TotalMarks</c> so the catalog can never disagree with the questions actually served.
    /// Refused once an attempt exists, because re-scoring history is not something an edit can do.
    /// </summary>
    Task<Result<QuestionBankResponse>> ReplaceBankAsync(
        int assessmentId, ReplaceQuestionBankRequest request, CancellationToken ct);
}
