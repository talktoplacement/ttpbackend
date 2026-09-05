using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CareerPlatform.Api.Features.Mentorship.Domain;

/// <summary>
/// A bookable mentor time slot. Ported from the legacy entity with identical columns; only the
/// base type (<see cref="AggregateRoot{TId}"/>) and namespace change (Req 9, 24.5).
/// </summary>
public class MentorSlot : AggregateRoot<int>
{
    [Required]
    public int MentorId { get; set; }

    [Required]
    public DateTime StartTimeUtc { get; set; }

    [Required]
    public DateTime EndTimeUtc { get; set; }

    public bool IsBooked { get; set; } = false;

    public string MeetingLink { get; set; } = string.Empty; // Google Meet / Jitsi / Zoom link

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("MentorId")]
    public Mentor? Mentor { get; set; }

    // 1:1 Booking relationship
    public MeetingBooking? Booking { get; set; }
}
