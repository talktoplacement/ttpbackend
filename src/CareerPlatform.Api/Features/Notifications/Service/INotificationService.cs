using CareerPlatform.Api.Features.Notifications.Dto;

namespace CareerPlatform.Api.Features.Notifications.Service;

public interface INotificationService
{
    Task<Result<IReadOnlyList<NotificationResponse>>> ListMineAsync(CancellationToken ct);
    Task<Result> MarkReadAsync(int id, CancellationToken ct);
    Task<Result> MarkAllReadAsync(CancellationToken ct);
    Task<Result> ClearAllAsync(CancellationToken ct);
    Task<Result<PublishNotificationResult>> PublishAsync(PublishNotificationRequest request, CancellationToken ct);
}
