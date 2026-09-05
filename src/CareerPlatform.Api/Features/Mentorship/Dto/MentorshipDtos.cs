using CareerPlatform.Api.Features.Mentorship.Domain;

namespace CareerPlatform.Api.Features.Mentorship.Dto;

// ── Response DTOs ────────────────────────────────────────────────────────────

/// <summary>Outward-facing mentor projection. Expertise is stored CSV and split for the client.</summary>
public sealed record MentorResponse(
    int Id,
    string Name,
    string Email,
    string Role,
    string Company,
    int YearsOfExperience,
    IReadOnlyList<string> Expertise,
    decimal Rating,
    int TotalReviews,
    decimal HourlyRateInr,
    string Bio,
    string? AvatarUrl,
    string Status,
    bool IsVerified)
{
    public static MentorResponse From(Mentor m)
    {
        ArgumentNullException.ThrowIfNull(m);
        var yoe = int.TryParse(new string((m.YearsOfExperience ?? string.Empty)
            .Where(char.IsDigit).ToArray()), out var n) ? n : 0;
        var expertise = string.IsNullOrWhiteSpace(m.Expertise)
            ? Array.Empty<string>()
            : m.Expertise.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return new MentorResponse(
            m.Id, m.Name, m.Email, m.Role, m.Company, yoe, expertise,
            m.Rating, m.TotalReviews, m.PricePerSession, m.Bio,
            string.IsNullOrWhiteSpace(m.AvatarUrl) ? null : m.AvatarUrl,
            m.VerificationStatus,
            string.Equals(m.VerificationStatus, "Verified", StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Anonymous-safe mentor projection for the public marketing catalog.
///
/// Deliberately a separate record from <see cref="MentorResponse"/> rather than a reuse: that one
/// carries the mentor's <c>Email</c>, which must never be served to unauthenticated callers. Only
/// verified + active mentors are ever projected into this shape, so verification flags are omitted
/// as well (they would always be constant).
/// </summary>
public sealed record PublicMentorResponse(
    int Id,
    string Name,
    string Role,
    string Company,
    int YearsOfExperience,
    IReadOnlyList<string> Expertise,
    decimal Rating,
    int TotalReviews,
    decimal HourlyRateInr,
    string Bio,
    string? AvatarUrl)
{
    public static PublicMentorResponse From(Mentor m)
    {
        ArgumentNullException.ThrowIfNull(m);
        var yoe = int.TryParse(new string((m.YearsOfExperience ?? string.Empty)
            .Where(char.IsDigit).ToArray()), out var n) ? n : 0;
        var expertise = string.IsNullOrWhiteSpace(m.Expertise)
            ? Array.Empty<string>()
            : m.Expertise.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return new PublicMentorResponse(
            m.Id, m.Name, m.Role, m.Company, yoe, expertise,
            m.Rating, m.TotalReviews, m.PricePerSession, m.Bio,
            string.IsNullOrWhiteSpace(m.AvatarUrl) ? null : m.AvatarUrl);
    }
}

/// <summary>Outward-facing mentor-slot projection.</summary>
public sealed record MentorSlotResponse(
    int Id, int MentorId, string StartTimeUtc, bool IsBooked, string? MeetingLink)
{
    public static MentorSlotResponse From(MentorSlot s)
    {
        ArgumentNullException.ThrowIfNull(s);
        return new MentorSlotResponse(
            s.Id, s.MentorId, s.StartTimeUtc.ToString("O"), s.IsBooked,
            string.IsNullOrWhiteSpace(s.MeetingLink) ? null : s.MeetingLink);
    }
}

/// <summary>Booking projection joining slot + mentor.</summary>
public sealed record MentorBookingResponse(
    int Id,
    int MentorId,
    string MentorName,
    string StudentEmail,
    string StudentName,
    string SlotTime,
    string? Notes,
    string? MeetingLink,
    string Status,
    /// <summary>True once the student has rated this session.</summary>
    bool HasReview,
    /// <summary>
    /// Whether the student may rate this session now. The eligibility RULE lives here rather than in
    /// the client, so the UI cannot offer a review the API would reject.
    /// </summary>
    bool CanReview)
{
    public static MentorBookingResponse From(
        MeetingBooking b, MentorSlot slot, Mentor mentor, bool hasReview = false)
    {
        var isCompleted = MeetingBookingStatus.Is(b.Status, MeetingBookingStatus.Completed);
        return new(b.Id, mentor.Id, mentor.Name, b.StudentEmail, b.StudentName,
            slot.StartTimeUtc.ToString("O"),
            string.IsNullOrWhiteSpace(b.TopicNote) ? null : b.TopicNote,
            string.IsNullOrWhiteSpace(b.MeetingUrl) ? null : b.MeetingUrl,
            b.Status,
            hasReview,
            isCompleted && !hasReview);
    }
}

/// <summary>Body for rating a completed 1:1 session.</summary>
public sealed record SubmitMentorReviewRequest(int Rating, string? Comment);

// ── Request DTOs ─────────────────────────────────────────────────────────────

/// <summary>Body for <c>POST /api/Mentorship/book</c>. Provide either SlotId or SlotTime.</summary>
public sealed record BookMentorSlotRequest(int MentorId, int? SlotId, string? SlotTime, string? Notes);

/// <summary>Body for <c>POST /api/Mentorship/admin/slots</c>. Duplicates against existing rows are skipped.</summary>
public sealed record CreateMentorSlotsRequest(int MentorId, List<string>? StartTimes);

/// <summary>Body for <c>POST /api/Dashboard/admin/mentors</c>.</summary>
public sealed record OnboardMentorRequest(
    string Name,
    string Email,
    string Role,
    string Company,
    int YearsOfExperience,
    List<string>? Expertise,
    decimal? HourlyRateInr,
    string? Bio,
    string? AvatarUrl);

/// <summary>Body for <c>PUT /api/Dashboard/admin/mentors</c>. Id in body, partial-update semantics.</summary>
public sealed record UpdateMentorRequest(
    int Id,
    string? Status,
    bool? IsActive,
    string? Name,
    string? Role,
    string? Company,
    int? YearsOfExperience,
    List<string>? Expertise,
    decimal? HourlyRateInr,
    string? Bio,
    string? AvatarUrl);
