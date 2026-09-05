using CareerPlatform.Api.Features.Mentorship.Domain;
using CareerPlatform.Api.Features.MentorPortal.Dto;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.MentorPortal.Service;

internal sealed class MentorPortalService : IMentorPortalService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;
    public MentorPortalService(AppDbContext db, ICurrentUser currentUser) { _db = db; _currentUser = currentUser; }

    public async Task<Result<MentorProfileResponse>> GetProfileAsync(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
            return Result.Failure<MentorProfileResponse>(Error.Unauthorized(
                "Mentor.Unauthorized", "An authenticated mentor is required."));
        var mentor = await _db.Mentors.AsNoTracking().FirstOrDefaultAsync(m => m.UserId == userId, ct);
        if (mentor is not null) return Result.Success(MentorProfileResponse.From(mentor));
        var profile = await _db.UserProfiles.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        return Result.Success(MentorProfileResponse.Unlinked(profile?.FullName ?? "", profile?.Email ?? ""));
    }

    public async Task<Result<MentorProfileResponse>> UpdateProfileAsync(UpdateMentorProfileRequest r, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
            return Result.Failure<MentorProfileResponse>(Error.Unauthorized(
                "Mentor.Unauthorized", "An authenticated mentor is required."));
        var expertise = string.Join(", ", (r.Expertise ?? Array.Empty<string>())
            .Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e.Trim()));
        var mentor = await _db.Mentors.FirstOrDefaultAsync(m => m.UserId == userId, ct);
        if (mentor is null)
        {
            var profile = await _db.UserProfiles.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
            mentor = new Mentor { UserId = userId, Email = profile?.Email ?? "", VerificationStatus = "Pending", IsActive = true, CreatedAt = DateTime.UtcNow };
            _db.Mentors.Add(mentor);
        }
        mentor.Name = r.Name.Trim();
        mentor.Company = r.Company?.Trim() ?? "";
        mentor.Role = r.Role?.Trim() ?? "";
        mentor.YearsOfExperience = r.YearsOfExperience?.Trim() ?? "";
        mentor.Bio = r.Bio?.Trim() ?? "";
        mentor.AvatarUrl = r.AvatarUrl?.Trim() ?? "";
        mentor.Expertise = expertise;
        mentor.PricePerSession = r.PricePerSession;
        await _db.SaveChangesAsync(ct);
        return Result.Success(MentorProfileResponse.From(mentor));
    }

    public async Task<Result<MentorOverviewResponse>> GetOverviewAsync(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
            return Result.Failure<MentorOverviewResponse>(Error.Unauthorized(
                "Mentor.Unauthorized", "An authenticated mentor is required."));
        var now = DateTime.UtcNow;
        var mentor = await _db.Mentors.AsNoTracking().FirstOrDefaultAsync(m => m.UserId == userId, ct);
        var mentorId = mentor?.Id;

        var upcoming = new List<MentorUpcomingSession>();
        var upcomingCount = 0;
        var hoursMentored = 0;
        var menteeIds = new HashSet<string>(StringComparer.Ordinal);

        if (mentorId is not null)
        {
            var bookings = await (
                from b in _db.MeetingBookings.AsNoTracking()
                join s in _db.MentorSlots.AsNoTracking() on b.SlotId equals s.Id
                where s.MentorId == mentorId && b.Status != MeetingBookingStatus.Cancelled
                select new { b.Id, b.StudentUserId, b.StudentName, b.TopicNote, s.StartTimeUtc, s.EndTimeUtc, b.Status, b.MeetingUrl })
                .ToListAsync(ct);
            upcomingCount = bookings.Count(x => x.StartTimeUtc >= now);
            upcoming = bookings.Where(x => x.StartTimeUtc >= now).OrderBy(x => x.StartTimeUtc).Take(5)
                .Select(x => new MentorUpcomingSession(x.Id, x.StudentName, x.TopicNote,
                    x.StartTimeUtc.ToString("O"), x.Status,
                    string.IsNullOrWhiteSpace(x.MeetingUrl) ? null : x.MeetingUrl))
                .ToList();
            hoursMentored = (int)Math.Round(bookings
                .Where(x => MeetingBookingStatus.Is(x.Status, MeetingBookingStatus.Completed))
                .Sum(x => (x.EndTimeUtc - x.StartTimeUtc).TotalHours));
            foreach (var x in bookings) menteeIds.Add(x.StudentUserId);
        }

        var resumeStudents = await _db.StudentResumeUploads.AsNoTracking()
            .Where(r => r.AssignedMentorUserId == userId).Select(r => r.StudentUserId).ToListAsync(ct);
        foreach (var id in resumeStudents) menteeIds.Add(id);

        // Same three sources as ListStudentsAsync, so the dashboard count cannot disagree with the
        // roster the mentor sees on the students page.
        if (mentorId is not null)
        {
            var assignedStudents = await _db.MentorAssignments.AsNoTracking()
                .Where(a => a.MentorId == mentorId && a.EndedAtUtc == null)
                .Select(a => a.StudentUserId).ToListAsync(ct);
            foreach (var id in assignedStudents) menteeIds.Add(id);
        }

        return Result.Success(new MentorOverviewResponse(
            upcomingCount, menteeIds.Count, mentor?.Rating ?? 0m, hoursMentored, upcoming));
    }

    public async Task<Result<IReadOnlyList<MentorSessionResponse>>> ListSessionsAsync(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
            return Result.Failure<IReadOnlyList<MentorSessionResponse>>(Error.Unauthorized(
                "Mentor.Unauthorized", "An authenticated mentor is required."));
        var mentorId = await _db.Mentors.AsNoTracking()
            .Where(m => m.UserId == userId).Select(m => (int?)m.Id).FirstOrDefaultAsync(ct);
        if (mentorId is null)
            return Result.Success<IReadOnlyList<MentorSessionResponse>>(Array.Empty<MentorSessionResponse>());
        var rows = await (
            from b in _db.MeetingBookings.AsNoTracking()
            join s in _db.MentorSlots.AsNoTracking() on b.SlotId equals s.Id
            where s.MentorId == mentorId
            orderby s.StartTimeUtc descending
            select new { b.Id, b.StudentName, b.StudentEmail, b.TopicNote, s.StartTimeUtc, s.EndTimeUtc, b.Status, b.MeetingUrl })
            .ToListAsync(ct);
        IReadOnlyList<MentorSessionResponse> items = rows.Select(x => new MentorSessionResponse(
            x.Id, x.StudentName, x.StudentEmail, x.TopicNote,
            x.StartTimeUtc.ToString("O"), x.EndTimeUtc.ToString("O"), x.Status,
            string.IsNullOrWhiteSpace(x.MeetingUrl) ? null : x.MeetingUrl)).ToList();
        return Result.Success(items);
    }

    public async Task<Result<MentorSessionResponse>> GetSessionAsync(int bookingId, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
            return Result.Failure<MentorSessionResponse>(Error.Unauthorized(
                "Mentor.Unauthorized", "An authenticated mentor is required."));
        var mentorId = await _db.Mentors.AsNoTracking()
            .Where(m => m.UserId == userId).Select(m => (int?)m.Id).FirstOrDefaultAsync(ct);
        if (mentorId is null)
            return Result.Failure<MentorSessionResponse>(SessionNotFound(bookingId));

        // The MentorId predicate is the authorization check: a booking id belonging to another
        // mentor resolves to nothing rather than leaking that student's details.
        var row = await (
            from b in _db.MeetingBookings.AsNoTracking()
            join s in _db.MentorSlots.AsNoTracking() on b.SlotId equals s.Id
            where b.Id == bookingId && s.MentorId == mentorId
            select new { b.Id, b.StudentName, b.StudentEmail, b.TopicNote, s.StartTimeUtc, s.EndTimeUtc, b.Status, b.MeetingUrl })
            .FirstOrDefaultAsync(ct);
        if (row is null)
            return Result.Failure<MentorSessionResponse>(SessionNotFound(bookingId));

        return Result.Success(new MentorSessionResponse(
            row.Id, row.StudentName, row.StudentEmail, row.TopicNote,
            row.StartTimeUtc.ToString("O"), row.EndTimeUtc.ToString("O"), row.Status,
            string.IsNullOrWhiteSpace(row.MeetingUrl) ? null : row.MeetingUrl));
    }

    private static Error SessionNotFound(int bookingId) => Error.NotFound(
        "Mentor.SessionNotFound", $"Session {bookingId} was not found among your bookings.");

    public async Task<Result<MentorSessionResponse>> CompleteSessionAsync(int bookingId, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
            return Result.Failure<MentorSessionResponse>(Error.Unauthorized(
                "Mentor.Unauthorized", "An authenticated mentor is required."));
        var mentorId = await _db.Mentors.AsNoTracking()
            .Where(m => m.UserId == userId).Select(m => (int?)m.Id).FirstOrDefaultAsync(ct);
        if (mentorId is null)
            return Result.Failure<MentorSessionResponse>(SessionNotFound(bookingId));

        // Tracked (no AsNoTracking) because this writes. The slot join carries the times we need for
        // both the guard below and the response.
        var pair = await (
            from b in _db.MeetingBookings
            join s in _db.MentorSlots.AsNoTracking() on b.SlotId equals s.Id
            where b.Id == bookingId && s.MentorId == mentorId
            select new { Booking = b, Slot = s })
            .FirstOrDefaultAsync(ct);
        if (pair is null)
            return Result.Failure<MentorSessionResponse>(SessionNotFound(bookingId));

        var booking = pair.Booking;

        if (MeetingBookingStatus.Is(booking.Status, MeetingBookingStatus.Completed))
        {
            // Idempotent: a double-click returns the same completed session rather than an error.
            return Result.Success(Project(booking, pair.Slot));
        }
        if (MeetingBookingStatus.Is(booking.Status, MeetingBookingStatus.Cancelled))
        {
            return Result.Failure<MentorSessionResponse>(Error.Conflict(
                "Mentor.SessionCancelled", "A cancelled session cannot be marked complete."));
        }
        // A session cannot have happened before it started. Guards against a mis-click inflating
        // "hours mentored" with time that has not elapsed.
        if (pair.Slot.StartTimeUtc > DateTime.UtcNow)
        {
            return Result.Failure<MentorSessionResponse>(Error.Validation(
                "Mentor.SessionNotStarted",
                "This session hasn't started yet, so it can't be marked complete."));
        }

        booking.Status = MeetingBookingStatus.Completed;
        await _db.SaveChangesAsync(ct);
        return Result.Success(Project(booking, pair.Slot));
    }

    /// <summary>Shared projection so every session-returning path emits an identical shape.</summary>
    private static MentorSessionResponse Project(MeetingBooking b, MentorSlot s) =>
        new(b.Id, b.StudentName, b.StudentEmail, b.TopicNote,
            s.StartTimeUtc.ToString("O"), s.EndTimeUtc.ToString("O"), b.Status,
            string.IsNullOrWhiteSpace(b.MeetingUrl) ? null : b.MeetingUrl);

    public async Task<Result<IReadOnlyList<MentorMenteeResponse>>> ListStudentsAsync(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
            return Result.Failure<IReadOnlyList<MentorMenteeResponse>>(Error.Unauthorized(
                "Mentor.Unauthorized", "An authenticated mentor is required."));
        var mentorId = await _db.Mentors.AsNoTracking()
            .Where(m => m.UserId == userId).Select(m => (int?)m.Id).FirstOrDefaultAsync(ct);
        var bookingAgg = new List<BookingAggRow>();
        var assignments = new List<AssignmentRow>();
        if (mentorId is not null)
        {
            var raw = await (
                from b in _db.MeetingBookings.AsNoTracking()
                join s in _db.MentorSlots.AsNoTracking() on b.SlotId equals s.Id
                where s.MentorId == mentorId
                group s by b.StudentUserId into g
                select new { StudentUserId = g.Key, SessionCount = g.Count(), LastSessionAt = g.Max(x => x.StartTimeUtc) })
                .ToListAsync(ct);
            bookingAgg = raw.Select(r => new BookingAggRow(r.StudentUserId, r.SessionCount, r.LastSessionAt)).ToList();

            // Explicit admin mappings. Previously ignored entirely, so a student an admin had paired
            // with this mentor never appeared in the mentor's roster until they happened to book a
            // session or have a resume routed — the two features silently disagreed.
            assignments = await _db.MentorAssignments.AsNoTracking()
                .Where(a => a.MentorId == mentorId && a.EndedAtUtc == null)
                .Select(a => new AssignmentRow(a.StudentUserId, a.CohortName))
                .ToListAsync(ct);
        }
        var resumes = await _db.StudentResumeUploads.AsNoTracking()
            .Where(r => r.AssignedMentorUserId == userId)
            .Select(r => new { r.StudentUserId, r.Id, r.OriginalFileName }).ToListAsync(ct);
        var studentIds = bookingAgg.Select(b => b.StudentUserId)
            .Union(assignments.Select(a => a.StudentUserId), StringComparer.Ordinal)
            .Union(resumes.Select(r => r.StudentUserId), StringComparer.Ordinal).ToList();
        if (studentIds.Count == 0)
            return Result.Success<IReadOnlyList<MentorMenteeResponse>>(Array.Empty<MentorMenteeResponse>());
        var profiles = await _db.UserProfiles.AsNoTracking()
            .Where(u => studentIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName, u.Email }).ToListAsync(ct);
        var items = studentIds.Select(id =>
        {
            var agg = bookingAgg.FirstOrDefault(b => b.StudentUserId == id);
            var resume = resumes.FirstOrDefault(r => r.StudentUserId == id);
            var profile = profiles.FirstOrDefault(p => p.Id == id);
            var assignment = assignments.FirstOrDefault(a => a.StudentUserId == id);
            return new MentorMenteeResponse(id, profile?.FullName ?? id, profile?.Email ?? "",
                agg?.SessionCount ?? 0, agg is null ? null : agg.LastSessionAt.ToString("O"),
                resume is not null, resume?.Id, resume?.OriginalFileName,
                assignment is not null, assignment?.CohortName);
        })
        // Admin-assigned students first — they are the roster the mentor is accountable for — then by
        // session volume, then alphabetically.
        .OrderByDescending(m => m.IsAssigned)
        .ThenByDescending(m => m.SessionCount)
        .ThenBy(m => m.FullName, StringComparer.OrdinalIgnoreCase).ToList();
        return Result.Success<IReadOnlyList<MentorMenteeResponse>>(items);
    }

    public async Task<Result<MentorMenteeDetailResponse>> GetStudentAsync(string studentUserId, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
            return Result.Failure<MentorMenteeDetailResponse>(Error.Unauthorized(
                "Mentor.Unauthorized", "An authenticated mentor is required."));
        if (string.IsNullOrWhiteSpace(studentUserId))
            return Result.Failure<MentorMenteeDetailResponse>(Error.Validation(
                "Mentor.InvalidStudent", "A student id is required."));
        var mentorId = await _db.Mentors.AsNoTracking()
            .Where(m => m.UserId == userId).Select(m => (int?)m.Id).FirstOrDefaultAsync(ct);
        var sessionRows = mentorId is null ? new List<MentorSessionResponse>() :
            (await (from b in _db.MeetingBookings.AsNoTracking()
                    join s in _db.MentorSlots.AsNoTracking() on b.SlotId equals s.Id
                    where s.MentorId == mentorId && b.StudentUserId == studentUserId
                    orderby s.StartTimeUtc descending
                    select new { b.Id, b.StudentName, b.StudentEmail, b.TopicNote, s.StartTimeUtc, s.EndTimeUtc, b.Status, b.MeetingUrl })
                    .ToListAsync(ct))
                .Select(x => new MentorSessionResponse(x.Id, x.StudentName, x.StudentEmail, x.TopicNote,
                    x.StartTimeUtc.ToString("O"), x.EndTimeUtc.ToString("O"), x.Status,
                    string.IsNullOrWhiteSpace(x.MeetingUrl) ? null : x.MeetingUrl)).ToList();
        var resume = await _db.StudentResumeUploads.AsNoTracking()
            .Where(r => r.StudentUserId == studentUserId && r.AssignedMentorUserId == userId)
            .Select(r => new { r.Id, r.OriginalFileName }).FirstOrDefaultAsync(ct);

        // An active admin assignment is itself sufficient authorization to view the mentee, and must
        // be checked here too — otherwise an assigned student with no bookings and no routed resume
        // appeared in the roster but 404'd when the mentor clicked through.
        var assignment = mentorId is null ? null : await _db.MentorAssignments.AsNoTracking()
            .Where(a => a.MentorId == mentorId && a.StudentUserId == studentUserId && a.EndedAtUtc == null)
            .Select(a => new { a.CohortName }).FirstOrDefaultAsync(ct);

        if (sessionRows.Count == 0 && resume is null && assignment is null)
            return Result.Failure<MentorMenteeDetailResponse>(Error.NotFound(
                "Mentor.MenteeNotFound", "This student is not assigned to you."));
        var profile = await _db.UserProfiles.AsNoTracking()
            .Where(u => u.Id == studentUserId).Select(u => new { u.FullName, u.Email }).FirstOrDefaultAsync(ct);
        var lastSessionAt = sessionRows.Count > 0 ? sessionRows[0].ScheduledAt : null;
        var mentee = new MentorMenteeResponse(studentUserId, profile?.FullName ?? studentUserId, profile?.Email ?? "",
            sessionRows.Count, lastSessionAt, resume is not null, resume?.Id, resume?.OriginalFileName,
            assignment is not null, assignment?.CohortName);
        return Result.Success(new MentorMenteeDetailResponse(mentee, sessionRows));
    }

    public async Task<Result<IReadOnlyList<MentorSlotItemResponse>>> ListSlotsAsync(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
            return Result.Failure<IReadOnlyList<MentorSlotItemResponse>>(Error.Unauthorized(
                "Mentor.Unauthorized", "An authenticated mentor is required."));
        var mentorId = await _db.Mentors.AsNoTracking()
            .Where(m => m.UserId == userId).Select(m => (int?)m.Id).FirstOrDefaultAsync(ct);
        if (mentorId is null)
            return Result.Success<IReadOnlyList<MentorSlotItemResponse>>(Array.Empty<MentorSlotItemResponse>());
        var slots = await _db.MentorSlots.AsNoTracking()
            .Where(s => s.MentorId == mentorId).OrderBy(s => s.StartTimeUtc).ToListAsync(ct);
        IReadOnlyList<MentorSlotItemResponse> items = slots.Select(MentorSlotItemResponse.From).ToList();
        return Result.Success(items);
    }

    public async Task<Result<MentorSlotItemResponse>> CreateSlotAsync(CreateMentorSlotRequest r, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
            return Result.Failure<MentorSlotItemResponse>(Error.Unauthorized(
                "Mentor.Unauthorized", "An authenticated mentor is required."));
        var mentorId = await _db.Mentors.Where(m => m.UserId == userId).Select(m => (int?)m.Id).FirstOrDefaultAsync(ct);
        if (mentorId is null)
            return Result.Failure<MentorSlotItemResponse>(Error.Validation(
                "Mentor.ProfileRequired", "Set up your mentor profile before adding availability slots."));
        var slot = new MentorSlot
        {
            MentorId = mentorId.Value,
            StartTimeUtc = DateTime.SpecifyKind(r.StartTimeUtc, DateTimeKind.Utc),
            EndTimeUtc = DateTime.SpecifyKind(r.EndTimeUtc, DateTimeKind.Utc),
            IsBooked = false, MeetingLink = r.MeetingLink?.Trim() ?? "",
            CreatedAt = DateTime.UtcNow,
        };
        _db.MentorSlots.Add(slot);
        await _db.SaveChangesAsync(ct);
        return Result.Success(MentorSlotItemResponse.From(slot));
    }

    public async Task<Result> DeleteSlotAsync(int id, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
            return Result.Failure(Error.Unauthorized("Mentor.Unauthorized", "An authenticated mentor is required."));
        var mentorId = await _db.Mentors.Where(m => m.UserId == userId).Select(m => (int?)m.Id).FirstOrDefaultAsync(ct);
        if (mentorId is null)
            return Result.Failure(Error.NotFound("Mentor.SlotNotFound", $"Slot {id} was not found."));
        var slot = await _db.MentorSlots.FirstOrDefaultAsync(s => s.Id == id && s.MentorId == mentorId, ct);
        if (slot is null)
            return Result.Failure(Error.NotFound("Mentor.SlotNotFound", $"Slot {id} was not found."));
        if (slot.IsBooked)
            return Result.Failure(Error.Conflict("Mentor.SlotBooked",
                "A booked slot cannot be deleted. Cancel the session first."));
        _db.MentorSlots.Remove(slot);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<MentorReviewResponse>>> ListReviewsAsync(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
            return Result.Failure<IReadOnlyList<MentorReviewResponse>>(Error.Unauthorized(
                "Mentor.Unauthorized", "An authenticated mentor is required."));
        var mentorId = await _db.Mentors.AsNoTracking()
            .Where(m => m.UserId == userId).Select(m => (int?)m.Id).FirstOrDefaultAsync(ct);
        if (mentorId is null)
            return Result.Success<IReadOnlyList<MentorReviewResponse>>(Array.Empty<MentorReviewResponse>());
        var reviews = await _db.MentorReviews.AsNoTracking()
            .Where(r => r.MentorId == mentorId)
            .OrderByDescending(r => r.CreatedAtUtc).ToListAsync(ct);
        IReadOnlyList<MentorReviewResponse> items = reviews.Select(MentorReviewResponse.From).ToList();
        return Result.Success(items);
    }

    private sealed record BookingAggRow(string StudentUserId, int SessionCount, DateTime LastSessionAt);

    private sealed record AssignmentRow(string StudentUserId, string? CohortName);
}
