using System.ComponentModel.DataAnnotations;
using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.StudentProfile.Domain;

/// <summary>
/// A student's notification and visibility settings. Exactly one row per user.
///
/// The two consent-bearing switches — <see cref="RecruiterVisibility"/> and
/// <see cref="PromotionalEmailsEnabled"/> — default to <c>false</c>. The UI they replace defaulted
/// both toggles to "on" without ever persisting anything, which displayed consent the student had
/// never given.
/// </summary>
public sealed class StudentPreferences : AuditableEntity<int>
{
    [Required, MaxLength(64)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>Transactional notifications (bookings, deadlines, results).</summary>
    public bool EmailNotificationsEnabled { get; set; } = true;

    /// <summary>Opt-in: allow partner companies to see this profile.</summary>
    public bool RecruiterVisibility { get; set; }

    public bool MentorshipRemindersEnabled { get; set; } = true;

    /// <summary>Opt-in marketing consent, honoured by the admin broadcast fan-out.</summary>
    public bool PromotionalEmailsEnabled { get; set; }

    [MaxLength(160)]
    public string? PreferredRole { get; set; }

    /// <summary>Comma-separated; denormalised because it is only ever read as a whole.</summary>
    [MaxLength(500)]
    public string? PreferredLocations { get; set; }

    /// <summary>
    /// The row a student starts with before they ever open the settings page. Created on demand by
    /// the service so a GET never 404s and never invents values in the response layer.
    /// </summary>
    public static StudentPreferences CreateDefault(string userId) => new()
    {
        UserId = userId,
        EmailNotificationsEnabled = true,
        RecruiterVisibility = false,
        MentorshipRemindersEnabled = true,
        PromotionalEmailsEnabled = false,
    };
}
