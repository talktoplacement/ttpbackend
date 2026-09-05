using CareerPlatform.Api.Features.Interviews.Dto;

namespace CareerPlatform.Api.Features.Interviews.Service;

/// <summary>Admin CRUD + public read for interview grading-rubric axes.</summary>
public interface IInterviewRubricService
{
    Task<Result<IReadOnlyList<InterviewRubricResponse>>> ListAsync(bool publishedOnly, CancellationToken ct);
    Task<Result<InterviewRubricResponse>> GetAsync(int id, CancellationToken ct);
    Task<Result<InterviewRubricResponse>> CreateAsync(UpsertInterviewRubricRequest request, CancellationToken ct);
    Task<Result<InterviewRubricResponse>> UpdateAsync(int id, UpsertInterviewRubricRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
}
