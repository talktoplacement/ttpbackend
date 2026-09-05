using CareerPlatform.Api.Features.Notifications.Domain;

namespace CareerPlatform.Api.Features.Notifications.Dto;

public sealed record NotificationResponse(
    int Id, string Type, string Title, string Body, bool IsRead, DateTime CreatedAt, string? ActionUrl)
{
    public static NotificationResponse From(Notification n)
    {
        ArgumentNullException.ThrowIfNull(n);
        return new NotificationResponse(n.Id, n.Type, n.Title, n.Body, n.IsRead, n.CreatedAt, n.ActionUrl);
    }
}

/// <summary>Body for <c>POST /api/v1/admin/notifications/publish</c>.</summary>
public sealed record PublishNotificationRequest(
    string? Type, string Title, string Body, string? TargetRole, string? ActionUrl);

/// <summary>Fan-out outcome — how many rows were created.</summary>
public sealed record PublishNotificationResult(int Recipients);
