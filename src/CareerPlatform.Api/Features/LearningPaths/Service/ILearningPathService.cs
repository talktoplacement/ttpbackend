using CareerPlatform.Api.Features.LearningPaths.Dto;

namespace CareerPlatform.Api.Features.LearningPaths.Service;

public interface ILearningPathService
{
    Task<Result<IReadOnlyList<LearningPathResponse>>> ListAsync(string? targetRole, bool publishedOnly, CancellationToken ct);
    Task<Result<LearningPathResponse>> GetAsync(string slug, CancellationToken ct);
    Task<Result<LearningPathResponse>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<LearningPathResponse>> CreateAsync(CreateLearningPathRequest request, CancellationToken ct);
    Task<Result<LearningPathResponse>> UpdateAsync(int id, UpdateLearningPathRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
}
