using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.Broadcasts.Domain;

/// <summary>
/// A record of an admin broadcast (notification bell or promotional email). Persisted so the
/// admin history view can list past sends with recipient counts — none of that data is
/// hardcoded on the frontend. Fan-out into per-user rows (Notifications table) happens in the
/// handler; this row is the audit-of-record for the batch itself.
/// </summary>
public sealed class Broadcast : AuditableEntity<int>
{
    public BroadcastType BroadcastType { get; set; } = BroadcastType.Notification;

    public string Heading { get; set; } = string.Empty;

    /// <summary>Audience filter — a plan name, "All Plans", or empty for everyone.</summary>
    public string TargetPlan { get; set; } = string.Empty;

    /// <summary>Optional interview-question text (notify-students only).</summary>
    public string? QuestionText { get; set; }

    /// <summary>Optional deep link paired with the question.</summary>
    public string? QuestionLink { get; set; }

    public string Message { get; set; } = string.Empty;

    /// <summary>Number of users the fan-out reached at send time (frozen snapshot).</summary>
    public int RecipientCount { get; set; }

    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;
}
