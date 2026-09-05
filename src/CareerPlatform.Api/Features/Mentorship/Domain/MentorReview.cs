using System.ComponentModel.DataAnnotations;
using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.Mentorship.Domain;

/// <summary>
/// A rating + written review a student leaves for a mentor after a completed 1:1 session.
/// The mentor-facing feedback page reads these rows; <see cref="Mentor.Rating"/> and
/// <see cref="Mentor.TotalReviews"/> are the aggregated snapshots derived from them.
/// </summary>
public sealed class MentorReview : AuditableEntity<int>
{
    /// <summary>FK to the <see cref="Mentor"/> catalog row the review is about.</summary>
    public int MentorId { get; set; }

    /// <summary>
    /// The completed <see cref="MeetingBooking"/> being rated.
    ///
    /// Nullable because rows created before the review flow existed have no session to point at. A
    /// filtered unique index on this column is what stops a student rating the same session twice —
    /// the service checks first, but only the constraint holds under a double-submit.
    /// </summary>
    public int? BookingId { get; set; }

    /// <summary>Author (student) — JWT subject / UserProfiles.Id.</summary>
    [Required]
    [MaxLength(64)]
    public string StudentUserId { get; set; } = string.Empty;

    /// <summary>Denormalized student display name captured at review time.</summary>
    [MaxLength(150)]
    public string StudentName { get; set; } = string.Empty;

    /// <summary>Star rating in the range 1..5.</summary>
    public int Rating { get; set; }

    [MaxLength(2000)]
    public string Comment { get; set; } = string.Empty;

    // Navigation
    public Mentor? Mentor { get; set; }
}
