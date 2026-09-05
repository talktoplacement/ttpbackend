using CareerPlatform.Api.Features.SubscriptionPlans.Dto;

namespace CareerPlatform.Api.Features.SubscriptionPlans.Service;

public interface ISubscriptionPlanService
{
    Task<Result<PaginatedResult<PlanResponse>>> ListAsync(int? page, int? pageSize, CancellationToken ct);
    Task<Result<IReadOnlyList<CatalogPlanResponse>>> ListActiveAsync(CancellationToken ct);
    Task<Result<PlanResponse>> GetAsync(int id, CancellationToken ct);
    Task<Result<PlanResponse>> CreateAsync(CreatePlanRequest request, CancellationToken ct);
    Task<Result<PlanResponse>> UpdateAsync(int id, UpdatePlanRequest request, CancellationToken ct);
    Task<Result<PlanResponse>> SetActiveAsync(int id, bool isActive, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
    Task<Result<EntitlementResponse>> GetEntitlementAsync(CancellationToken ct);
}
