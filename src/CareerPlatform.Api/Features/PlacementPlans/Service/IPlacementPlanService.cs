using CareerPlatform.Api.Features.PlacementPlans.Dto;

namespace CareerPlatform.Api.Features.PlacementPlans.Service;

public interface IPlacementPlanService
{
    Task<Result<IReadOnlyList<PlacementPlanResponse>>> ListAsync(bool publishedOnly, CancellationToken ct);
    Task<Result<PlacementPlanResponse>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<PlacementPlanResponse>> CreateAsync(CreatePlacementPlanRequest request, CancellationToken ct);
    Task<Result<PlacementPlanResponse>> UpdateAsync(int id, UpdatePlacementPlanRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
}
