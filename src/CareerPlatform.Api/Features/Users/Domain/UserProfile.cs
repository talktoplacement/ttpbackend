using CareerPlatform.Api.Features.Offers.Domain;
using CareerPlatform.Api.Features.Students.Domain;

namespace CareerPlatform.Api.Features.Users.Domain;

/// <summary>
/// Identity/profile aggregate. Keyed by the Supabase Auth UUID (string PK, not DB-generated).
/// Ported from the legacy <c>backend.Shared.Persistence.Entities.UserProfile</c> with identical
/// columns; only the base type (<see cref="AggregateRoot{TId}"/>) and namespace change (Req 9, 24.5).
/// </summary>
public class UserProfile : AggregateRoot<string>
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    /// <summary>
    /// BCrypt-hashed password. Null for users that authenticated through an external identity
    /// provider (e.g. an early Supabase-created row) so the app can be migrated incrementally.
    /// Populated by the custom-auth registration flow after OTP verification.
    /// </summary>
    public string? PasswordHash { get; set; }

    // --- Password-reset OTP fields (nullable; populated only during an active reset flow) ---

    /// <summary>Hex-lowercase HMAC-SHA256 of the currently-issued reset code. Null when no reset is pending.</summary>
    [System.ComponentModel.DataAnnotations.MaxLength(128)]
    public string? PasswordResetOtpHash { get; set; }

    /// <summary>UTC expiry of the current reset code. Null when no reset is pending.</summary>
    public DateTime? PasswordResetOtpExpiresAt { get; set; }

    /// <summary>Wrong verification attempts remaining before the code is invalidated.</summary>
    public int PasswordResetOtpAttemptsRemaining { get; set; }

    /// <summary>UTC timestamp of the last reset-code send — used to enforce the resend cooldown.</summary>
    public DateTime? PasswordResetOtpLastSentAt { get; set; }

    public string Role { get; set; } = "Student"; // "Student" or "Admin"
    public string PlanName { get; set; } = "Free"; // "Free", "Monthly (Pro)", "Yearly (Premium)"

    // Optional contact/organisational profile fields, self-service editable via PUT /api/me.
    // Nullable so the additive migration is non-destructive to existing rows (Req 13.1).
    public string? Phone { get; set; }
    public string? Designation { get; set; }
    public string? Department { get; set; }
    public string? YearsOfExperience { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<ProgressLog> ProgressLogs { get; set; } = new List<ProgressLog>();
    public ICollection<OfferLetter> OfferLetters { get; set; } = new List<OfferLetter>();
}
