using System.ComponentModel.DataAnnotations;

namespace CareerPlatform.Api.Features.Notifications.Domain;

/// <summary>
/// An in-app notification for a user (e.g. enrollment confirmed, meeting booked). Ported from the
/// legacy entity with identical columns; only the base type (<see cref="AggregateRoot{TId}"/>) and
/// namespace change (Req 9, 24.5).
/// </summary>
public class Notification : AggregateRoot<int>
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(60)]
    public string Type { get; set; } = string.Empty; // e.g. "EnrollmentCreated"

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public bool IsRead { get; set; } = false;

    /// <summary>
    /// User-scoped dismissal flag (soft-clear). When true the row is hidden from the user's feed
    /// but retained for auditability. Set by the "clear all" flow; independent of read state.
    /// </summary>
    public bool IsDismissed { get; set; } = false;

    /// <summary>Optional deep-link the notification card opens on click.</summary>
    [MaxLength(500)]
    public string? ActionUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
