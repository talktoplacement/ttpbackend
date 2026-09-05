using CareerPlatform.Api.Features.Meetings.Domain;

namespace CareerPlatform.Api.Features.Meetings.Dto;

public sealed record MeetingResponse(
    int Id,
    string Title,
    string Type,
    string? MentorName,
    string? MentorCompany,
    string? StudentName,
    string? StudentEmail,
    string? CohortTarget,
    string? TargetAudienceLabel,
    int? AttendeeCount,
    string ScheduledAt,
    int DurationMinutes,
    string Status,
    string? MeetUrl,
    string? Notes)
{
    public static MeetingResponse From(Meeting m)
    {
        ArgumentNullException.ThrowIfNull(m);
        return new MeetingResponse(
            m.Id, m.Title, m.MeetingType, m.MentorName, m.MentorCompany,
            m.StudentName, m.StudentEmail, m.CohortTarget, m.TargetAudienceLabel,
            m.AttendeeCount == 0 ? null : m.AttendeeCount,
            m.ScheduledAtUtc.ToString("O"), m.DurationMinutes, m.Status,
            string.IsNullOrEmpty(m.MeetUrl) ? null : m.MeetUrl, m.Notes);
    }
}

/// <summary>Body for <c>POST /api/v1/admin/meetings</c>.</summary>
public sealed record ScheduleMeetingRequest(
    string Title, string Type,
    string? MentorName, string? MentorCompany,
    string? StudentName, string? StudentEmail,
    string? CohortTarget, string? TargetAudienceLabel,
    int? AttendeeCount, string ScheduledAt, int DurationMinutes,
    string MeetUrl, string? Notes);

/// <summary>Body for <c>PUT /api/v1/admin/meetings</c>. Id in body preserves the frontend contract.</summary>
public sealed record UpdateMeetingRequest(int Id, string? Status, string? ScheduledAt, string? MeetUrl);
