using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.Meetings.Domain;

/// <summary>
/// An admin-scheduled meeting or cohort webinar. Distinct from <c>MeetingBooking</c>, which
/// records a student booking against a mentor slot: this row is authored by an admin from the
/// admin meetings page and represents either a single-student session or a plan-scoped cohort
/// broadcast. All fields render on the admin meetings grid; none are hardcoded on the frontend.
/// </summary>
public sealed class Meeting : AuditableEntity<int>
{
    public string Title { get; set; } = string.Empty;

    /// <summary>1:1 Mentorship / Mock Interview / Prep Guidance / Resume Review / Cohort Webinar.</summary>
    public string MeetingType { get; set; } = "1:1 Mentorship";

    /// <summary>Optional mentor display name (denormalized snapshot for the admin grid).</summary>
    public string? MentorName { get; set; }
    public string? MentorCompany { get; set; }

    /// <summary>Populated for single-student sessions; empty for cohort broadcasts.</summary>
    public string? StudentName { get; set; }
    public string? StudentEmail { get; set; }

    /// <summary>ALL_PAID / MONTHLY_PRO / YEARLY_PREMIUM / ALL_STUDENTS / SINGLE — resolved on send.</summary>
    public string? CohortTarget { get; set; }

    public string? TargetAudienceLabel { get; set; }

    public int AttendeeCount { get; set; }

    public DateTime ScheduledAtUtc { get; set; }

    public int DurationMinutes { get; set; }

    /// <summary>Scheduled / In Progress / Completed / Cancelled.</summary>
    public string Status { get; set; } = "Scheduled";

    public string MeetUrl { get; set; } = string.Empty;

    public string? Notes { get; set; }
}
