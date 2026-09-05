using CareerPlatform.Api.Features.Mentorship.Domain;

namespace CareerPlatform.Api.Features.MentorPortal.Dto;

public sealed record MentorProfileResponse(
    int? Id, bool IsLinked, string Name, string Email, string Company, string Role,
    string YearsOfExperience, string Bio, string AvatarUrl,
    IReadOnlyList<string> Expertise, decimal PricePerSession,
    string VerificationStatus, decimal Rating, int TotalReviews)
{
    public static MentorProfileResponse From(Mentor m)
    {
        ArgumentNullException.ThrowIfNull(m);
        var expertise = string.IsNullOrWhiteSpace(m.Expertise)
            ? Array.Empty<string>()
            : m.Expertise.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return new MentorProfileResponse(
            m.Id, true, m.Name, m.Email, m.Company, m.Role, m.YearsOfExperience,
            m.Bio, m.AvatarUrl, expertise, m.PricePerSession, m.VerificationStatus,
            m.Rating, m.TotalReviews);
    }
    public static MentorProfileResponse Unlinked(string name, string email) =>
        new(null, false, name, email, "", "", "", "", "", Array.Empty<string>(), 0m, "Pending", 0m, 0);
}

public sealed record MentorUpcomingSession(int BookingId, string StudentName, string Topic,
    string ScheduledAt, string Status, string? MeetingUrl);

public sealed record MentorOverviewResponse(int UpcomingSessions, int TotalMentees,
    decimal AverageRating, int HoursMentored, IReadOnlyList<MentorUpcomingSession> Upcoming);

public sealed record MentorSessionResponse(int Id, string StudentName, string StudentEmail,
    string Topic, string ScheduledAt, string EndsAt, string Status, string? MeetingUrl);

/// <summary>
/// A student in the mentor's roster.
///
/// A student can reach the roster three ways — an explicit admin assignment, a booked session, or an
/// assigned resume — so <see cref="IsAssigned"/> and <see cref="CohortName"/> report whether the
/// pairing is a deliberate admin mapping rather than something inferred from activity. Without this
/// the UI could not honestly label the list "mapped students".
/// </summary>
public sealed record MentorMenteeResponse(string StudentUserId, string FullName, string Email,
    int SessionCount, string? LastSessionAt, bool HasResume, int? ResumeUploadId, string? ResumeFileName,
    bool IsAssigned, string? CohortName);

public sealed record MentorMenteeDetailResponse(MentorMenteeResponse Mentee, IReadOnlyList<MentorSessionResponse> Sessions);

public sealed record MentorReviewResponse(int Id, string StudentName, int Rating, string Comment, string CreatedAt)
{
    public static MentorReviewResponse From(MentorReview r)
    {
        ArgumentNullException.ThrowIfNull(r);
        return new MentorReviewResponse(r.Id, r.StudentName, r.Rating, r.Comment, r.CreatedAtUtc.ToString("O"));
    }
}

public sealed record MentorSlotItemResponse(int Id, string StartTimeUtc, string EndTimeUtc, bool IsBooked, string? MeetingLink)
{
    public static MentorSlotItemResponse From(MentorSlot s)
    {
        ArgumentNullException.ThrowIfNull(s);
        return new MentorSlotItemResponse(s.Id, s.StartTimeUtc.ToString("O"),
            s.EndTimeUtc.ToString("O"), s.IsBooked,
            string.IsNullOrWhiteSpace(s.MeetingLink) ? null : s.MeetingLink);
    }
}

public sealed record UpdateMentorProfileRequest(
    string Name, string Company, string Role, string YearsOfExperience,
    string Bio, string AvatarUrl, IReadOnlyList<string> Expertise, decimal PricePerSession);

public sealed record CreateMentorSlotRequest(DateTime StartTimeUtc, DateTime EndTimeUtc, string? MeetingLink);
