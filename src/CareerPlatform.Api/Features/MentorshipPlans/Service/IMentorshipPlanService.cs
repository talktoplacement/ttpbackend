using CareerPlatform.Api.Features.MentorshipPlans.Dto;

namespace CareerPlatform.Api.Features.MentorshipPlans.Service;

public interface IMentorshipPlanService
{
    Task<Result<IReadOnlyList<MentorshipPlanResponse>>> ListAsync(bool publishedOnly, CancellationToken ct);
    Task<Result<MentorshipPlanResponse>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<MentorshipPlanResponse>> CreateAsync(CreateMentorshipPlanRequest request, CancellationToken ct);
    Task<Result<MentorshipPlanResponse>> UpdateAsync(int id, UpdateMentorshipPlanRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
}
