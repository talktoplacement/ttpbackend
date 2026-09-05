using System.Globalization;
using CareerPlatform.Api.Features.Meetings.Domain;
using CareerPlatform.Api.Features.Meetings.Dto;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.Meetings.Service;

/// <summary>Admin meetings workflow. Ports the 4 legacy MediatR handlers verbatim.</summary>
internal sealed class MeetingService : IMeetingService
{
    private readonly AppDbContext _db;
    public MeetingService(AppDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<MeetingResponse>>> ListAsync(CancellationToken ct)
    {
        // Bounded by the shared pagination cap rather than a magic literal, so this list can never
        // materialize an unbounded table scan.
        var rows = await _db.Meetings.AsNoTracking()
            .OrderByDescending(m => m.ScheduledAtUtc)
            .ThenByDescending(m => m.Id)
            .Take(PaginationRequest.MaxPageSize)
            .ToListAsync(ct);
        IReadOnlyList<MeetingResponse> items = rows.Select(MeetingResponse.From).ToList();
        return Result.Success(items);
    }

    public async Task<Result<MeetingResponse>> ScheduleAsync(ScheduleMeetingRequest r, CancellationToken ct)
    {
        var scheduledAt = DateTime.Parse(r.ScheduledAt, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

        var meeting = new Meeting
        {
            Title = r.Title.Trim(),
            MeetingType = r.Type,
            MentorName = Trim(r.MentorName),
            MentorCompany = Trim(r.MentorCompany),
            StudentName = Trim(r.StudentName),
            StudentEmail = Trim(r.StudentEmail)?.ToLowerInvariant(),
            CohortTarget = Trim(r.CohortTarget),
            TargetAudienceLabel = Trim(r.TargetAudienceLabel),
            AttendeeCount = r.AttendeeCount ?? 0,
            ScheduledAtUtc = scheduledAt,
            DurationMinutes = r.DurationMinutes,
            MeetUrl = r.MeetUrl.Trim(),
            Notes = Trim(r.Notes),
            Status = "Scheduled",
        };
        _db.Meetings.Add(meeting);
        await _db.SaveChangesAsync(ct);
        return Result.Success(MeetingResponse.From(meeting));
    }

    public async Task<Result<MeetingResponse>> UpdateAsync(UpdateMeetingRequest r, CancellationToken ct)
    {
        var meeting = await _db.Meetings.FirstOrDefaultAsync(m => m.Id == r.Id, ct);
        if (meeting is null)
        {
            return Result.Failure<MeetingResponse>(Error.NotFound(
                "Meeting.NotFound", $"Meeting {r.Id} was not found."));
        }
        if (r.Status is not null) meeting.Status = r.Status;
        if (r.ScheduledAt is not null)
        {
            meeting.ScheduledAtUtc = DateTime.Parse(r.ScheduledAt, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        }
        if (r.MeetUrl is not null) meeting.MeetUrl = r.MeetUrl;
        await _db.SaveChangesAsync(ct);
        return Result.Success(MeetingResponse.From(meeting));
    }

    public async Task<Result> CancelAsync(int id, CancellationToken ct)
    {
        var meeting = await _db.Meetings.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (meeting is null)
        {
            return Result.Failure(Error.NotFound("Meeting.NotFound", $"Meeting {id} was not found."));
        }
        meeting.Status = "Cancelled";
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static string? Trim(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}
