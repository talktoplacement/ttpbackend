using CareerPlatform.Api.Features.Broadcasts.Dto;

namespace CareerPlatform.Api.Features.Broadcasts.Service;

public interface IBroadcastService
{
    Task<Result<IReadOnlyList<BroadcastResponse>>> ListAsync(string? type, CancellationToken ct);
    Task<Result<RecipientCountResult>> GetRecipientCountAsync(string? targetPlan, CancellationToken ct);
    Task<Result<IReadOnlyList<BroadcastAudienceTarget>>> ListAudienceTargetsAsync(CancellationToken ct);

    /// <summary>
    /// Today's <c>Notification</c> broadcasts visible to the signed-in student, i.e. those targeted
    /// at every plan or at the student's own current plan.
    /// </summary>
    Task<Result<IReadOnlyList<BroadcastResponse>>> ListTodayForCurrentStudentAsync(CancellationToken ct);
    Task<Result<SendBroadcastResult>> SendAsync(SendBroadcastRequest request, CancellationToken ct);
}
