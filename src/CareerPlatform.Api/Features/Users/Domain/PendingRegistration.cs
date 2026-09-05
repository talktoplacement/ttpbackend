using System.ComponentModel.DataAnnotations;

namespace CareerPlatform.Api.Features.Users.Domain;

/// <summary>
/// In-flight self-service registration record. Holds the applicant's profile fields and a hashed
/// one-time code until the applicant verifies the code (at which point the row is promoted into
/// a <see cref="UserProfile"/> and deleted). Never carries a plaintext code — only its HMAC hash.
/// Keyed by <see cref="Email"/> so re-registration for the same address updates in place instead
/// of accumulating rows.
/// </summary>
public class PendingRegistration : AggregateRoot<int>
{
    [Required]
    [EmailAddress]
    [MaxLength(320)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(32)]
    public string MobileNumber { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? YearsOfExperience { get; set; }

    /// <summary>Requested role — validated to be "Student" or "Mentor" at the API boundary.</summary>
    [Required]
    [MaxLength(32)]
    public string IntendedRole { get; set; } = "Student";

    /// <summary>BCrypt-hashed password entered in step 2 of the wizard.</summary>
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Hex-lowercase HMAC-SHA256 of the plaintext OTP. Plaintext never persisted.</summary>
    [Required]
    [MaxLength(128)]
    public string OtpHash { get; set; } = string.Empty;

    /// <summary>UTC expiry timestamp of the current OTP. After this instant the code is dead.</summary>
    public DateTime OtpExpiresAt { get; set; }

    /// <summary>Wrong attempts remaining before the code is invalidated and a resend is required.</summary>
    public int OtpAttemptsRemaining { get; set; }

    /// <summary>UTC timestamp of the last OTP send — used to enforce the resend cooldown.</summary>
    public DateTime OtpLastSentAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
