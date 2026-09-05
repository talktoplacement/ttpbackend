using CareerPlatform.Api.Features.Practice.Dto;

namespace CareerPlatform.Api.Features.Practice.Service;

public interface IPracticeService
{
    Task<Result<IReadOnlyList<PracticeQuestionResponse>>> ListAsync(string? category, bool publishedOnly, CancellationToken ct);
    Task<Result<PracticeQuestionResponse>> GetAsync(string slug, CancellationToken ct);
    Task<Result<PracticeQuestionResponse>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<PracticeQuestionResponse>> CreateAsync(CreatePracticeQuestionRequest request, CancellationToken ct);
    Task<Result<PracticeQuestionResponse>> UpdateAsync(int id, UpdatePracticeQuestionRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);

    Task<Result<IReadOnlyList<PracticeBookmarkResponse>>> ListMyBookmarksAsync(CancellationToken ct);
    Task<Result<PracticeBookmarkResponse>> AddBookmarkAsync(int questionId, string? notes, CancellationToken ct);
    Task<Result> RemoveBookmarkAsync(int questionId, CancellationToken ct);
}
