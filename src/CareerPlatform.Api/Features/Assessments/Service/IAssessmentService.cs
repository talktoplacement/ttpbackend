using CareerPlatform.Api.Features.Assessments.Dto;

namespace CareerPlatform.Api.Features.Assessments.Service;

public interface IAssessmentService
{
    Task<Result<IReadOnlyList<AssessmentResponse>>> ListAsync(string? category, bool publishedOnly, CancellationToken ct);
    Task<Result<AssessmentResponse>> GetAsync(string slug, CancellationToken ct);

    /// <summary>
    /// Loads by primary key, ignoring the published filter, so an admin can edit a draft. Kept
    /// distinct from <see cref="GetAsync"/> rather than adding a <c>publishedOnly</c> flag there:
    /// the student route must never be able to widen its own visibility.
    /// </summary>
    Task<Result<AssessmentResponse>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<AssessmentResponse>> CreateAsync(CreateAssessmentRequest request, CancellationToken ct);
    Task<Result<AssessmentResponse>> UpdateAsync(int id, UpdateAssessmentRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);

    Task<Result<IReadOnlyList<AssessmentAttemptResponse>>> ListMyAttemptsAsync(CancellationToken ct);
    Task<Result<AssessmentAttemptResponse>> GetMyAttemptAsync(int id, CancellationToken ct);
    Task<Result<AssessmentAttemptResponse>> StartAttemptAsync(string slug, CancellationToken ct);

    // Submission lives on IAssessmentRunnerService: grading is server-side only, so there is no
    // contract here that could accept a client-supplied score.
}
