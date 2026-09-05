using System.ComponentModel.DataAnnotations;

namespace CareerPlatform.Api.Features.Mentorship.Domain;

/// <summary>
/// Mentor profile. Ported from the legacy entity with identical columns; only the base type
/// (<see cref="AggregateRoot{TId}"/>) and namespace change (Req 9, 24.5).
/// </summary>
public class Mentor : AggregateRoot<int>
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Company { get; set; } = string.Empty; // e.g. "Google", "Amazon"

    [Required]
    [MaxLength(100)]
    public string Role { get; set; } = string.Empty; // e.g. "Senior SDE", "Engineering Manager"

    public string YearsOfExperience { get; set; } = string.Empty;

    public string Bio { get; set; } = string.Empty;

    public string AvatarUrl { get; set; } = string.Empty;

    public string Expertise { get; set; } = "DSA, System Design"; // Comma separated or JSON

    public decimal PricePerSession { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    /// <summary>Admin verification state: <c>Verified</c> / <c>Pending</c> / <c>Suspended</c>.</summary>
    [MaxLength(20)]
    public string VerificationStatus { get; set; } = "Pending";

    /// <summary>Average rating across student reviews (0..5). Computed elsewhere; snapshotted here.</summary>
    public decimal Rating { get; set; } = 0m;

    public int TotalReviews { get; set; } = 0;

    /// <summary>
    /// Links this mentor catalog row to the authenticated mentor user (JWT subject / UserProfiles.Id).
    /// Nullable: admin-onboarded catalog rows may be unlinked until a mentor user claims/edits the
    /// profile. All mentor-self endpoints resolve the caller's row via <c>UserId == currentUser.UserId</c>.
    /// </summary>
    [MaxLength(64)]
    public string? UserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<MentorSlot> Slots { get; set; } = new List<MentorSlot>();
}
