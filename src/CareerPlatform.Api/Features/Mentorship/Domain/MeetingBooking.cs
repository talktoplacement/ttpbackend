using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CareerPlatform.Api.Features.Mentorship.Domain;

/// <summary>
/// A confirmed 1:1 meeting booking against a <see cref="MentorSlot"/>. Ported from the legacy
/// entity with identical columns; only the base type (<see cref="AggregateRoot{TId}"/>) and
/// namespace change (Req 9, 24.5).
/// </summary>
public class MeetingBooking : AggregateRoot<int>
{
    [Required]
    public int SlotId { get; set; }

    [Required]
    public string StudentUserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string StudentName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string StudentEmail { get; set; } = string.Empty;

    public string TopicNote { get; set; } = string.Empty; // e.g. "Mock interview for Google L4, focus on Graphs & Dynamic Programming"

    public string ResumeUrl { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    /// <summary>One of <see cref="MeetingBookingStatus"/>.</summary>
    public string Status { get; set; } = MeetingBookingStatus.Scheduled;

    public string MeetingUrl { get; set; } = string.Empty;

    public DateTime BookedAtUtc { get; set; } = DateTime.UtcNow;

    [ForeignKey("SlotId")]
    public MentorSlot? Slot { get; set; }
}
