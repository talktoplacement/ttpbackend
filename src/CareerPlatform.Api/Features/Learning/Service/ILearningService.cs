using CareerPlatform.Api.Features.Learning.Dto;

namespace CareerPlatform.Api.Features.Learning.Service;

public interface ILearningService
{
    Task<Result<IReadOnlyList<LearningProgressResponse>>> ListMineAsync(string? resourceType, CancellationToken ct);
    Task<Result<LearningProgressSummary>> GetSummaryAsync(CancellationToken ct);
    Task<Result<LearningProgressResponse>> UpsertAsync(
        string resourceType, int resourceId, UpsertProgressRequest request, CancellationToken ct);
}
