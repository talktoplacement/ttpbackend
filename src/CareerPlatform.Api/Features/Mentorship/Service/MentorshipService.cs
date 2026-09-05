using System.Globalization;
using System.Security.Claims;
using CareerPlatform.Api.Features.Mentorship.Domain;
using CareerPlatform.Api.Features.Mentorship.Dto;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.Mentorship.Service;

/// <summary>Mentorship workflow. Ports the 10 legacy MediatR handlers verbatim into methods.</summary>
internal sealed class MentorshipService : IMentorshipService
{
    private const int DefaultSlotMinutes = 60;

    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IHttpContextAccessor _httpContext;

    public MentorshipService(AppDbContext db, ICurrentUser currentUser, IHttpContextAccessor httpContext)
    {
        _db = db;
        _currentUser = currentUser;
        _httpContext = httpContext;
    }

    // ── Catalog ─────────────────────────────────────────────────────────────

    public async Task<Result<IReadOnlyList<MentorResponse>>> ListMentorsAsync(
        string? expertise, bool activeOnly, CancellationToken ct)
    {
        var rows = await BuildCatalogQuery(expertise, activeOnly).ToListAsync(ct);
        IReadOnlyList<MentorResponse> items = rows.Select(MentorResponse.From).ToList();
        return Result.Success(items);
    }

    public async Task<Result<IReadOnlyList<PublicMentorResponse>>> ListPublicMentorsAsync(
        string? expertise, CancellationToken ct)
    {
        // Always verified + active: the public catalog never exposes pending/suspended mentors.
        var rows = await BuildCatalogQuery(expertise, activeOnly: true)
            .ToListAsync(ct);
        IReadOnlyList<PublicMentorResponse> items = rows.Select(PublicMentorResponse.From).ToList();
        return Result.Success(items);
    }

    /// <summary>
    /// Shared catalog query used by both the authenticated and public listings, so filtering,
    /// ordering, and the page cap can never drift between them.
    /// </summary>
    private IQueryable<Mentor> BuildCatalogQuery(string? expertise, bool activeOnly)
    {
        var query = _db.Mentors.AsNoTracking();
        if (activeOnly)
        {
            query = query.Where(m => m.IsActive && m.VerificationStatus == "Verified");
        }
        if (!string.IsNullOrWhiteSpace(expertise))
        {
            var needle = expertise.Trim();
            query = query.Where(m => EF.Functions.ILike(m.Expertise, $"%{needle}%"));
        }
        return query
            .OrderByDescending(m => m.Rating)
            .ThenBy(m => m.Id)
            .Take(PaginationRequest.MaxPageSize);
    }

    public async Task<Result<IReadOnlyList<MentorSlotResponse>>> ListMentorSlotsAsync(
        int mentorId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var slots = await _db.MentorSlots.AsNoTracking()
            .Where(s => s.MentorId == mentorId && s.StartTimeUtc > now)
            .OrderBy(s => s.StartTimeUtc)
            .ToListAsync(ct);
        IReadOnlyList<MentorSlotResponse> items = slots.Select(MentorSlotResponse.From).ToList();
        return Result.Success(items);
    }

    // ── Booking (student) ───────────────────────────────────────────────────

    public async Task<Result<MentorBookingResponse>> BookAsync(BookMentorSlotRequest body, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure<MentorBookingResponse>(Error.Unauthorized(
                "Booking.Unauthorized", "An authenticated user is required to book a slot."));
        }
        var mentor = await _db.Mentors.FirstOrDefaultAsync(m => m.Id == body.MentorId, ct);
        if (mentor is null)
        {
            return Result.Failure<MentorBookingResponse>(Error.NotFound(
                "Mentor.NotFound", $"Mentor {body.MentorId} was not found."));
        }
        MentorSlot? slot = null;
        if (body.SlotId.HasValue)
        {
            slot = await _db.MentorSlots.FirstOrDefaultAsync(
                s => s.Id == body.SlotId.Value && s.MentorId == body.MentorId, ct);
        }
        else if (!string.IsNullOrWhiteSpace(body.SlotTime))
        {
            var when = DateTime.Parse(body.SlotTime, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            slot = await _db.MentorSlots.FirstOrDefaultAsync(
                s => s.MentorId == body.MentorId && s.StartTimeUtc == when, ct);
        }
        if (slot is null)
        {
            return Result.Failure<MentorBookingResponse>(Error.NotFound(
                "Slot.NotFound", "The requested slot was not found for this mentor."));
        }
        if (slot.IsBooked)
        {
            return Result.Failure<MentorBookingResponse>(Error.Validation(
                "Slot.AlreadyBooked", "This slot has already been booked. Please pick another."));
        }
        var profile = await _db.UserProfiles.FirstOrDefaultAsync(u => u.Id == userId, ct);
        var email = profile?.Email
            ?? _httpContext.HttpContext?.User?.FindFirstValue(ClaimTypes.Email)
            ?? string.Empty;
        var name = profile?.FullName ?? string.Empty;

        slot.IsBooked = true;
        var booking = new MeetingBooking
        {
            SlotId = slot.Id,
            StudentUserId = userId,
            StudentName = name,
            StudentEmail = email,
            TopicNote = body.Notes?.Trim() ?? string.Empty,
            ResumeUrl = string.Empty,
            Status = MeetingBookingStatus.Scheduled,
            MeetingUrl = slot.MeetingLink,
            BookedAtUtc = DateTime.UtcNow,
        };
        _db.MeetingBookings.Add(booking);
        await _db.SaveChangesAsync(ct);
        return Result.Success(MentorBookingResponse.From(booking, slot, mentor));
    }

    public async Task<Result<IReadOnlyList<MentorBookingResponse>>> ListMyBookingsAsync(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure<IReadOnlyList<MentorBookingResponse>>(Error.Unauthorized(
                "Booking.Unauthorized", "An authenticated user is required to read bookings."));
        }
        var rows = await (from b in _db.MeetingBookings.AsNoTracking()
                          join s in _db.MentorSlots.AsNoTracking() on b.SlotId equals s.Id
                          join m in _db.Mentors.AsNoTracking() on s.MentorId equals m.Id
                          where b.StudentUserId == userId
                          orderby s.StartTimeUtc descending
                          select new { b, s, m })
            .Take(PaginationRequest.MaxPageSize)
            .ToListAsync(ct);

        // One query for the whole page's review state rather than a lookup per booking.
        var bookingIds = rows.Select(r => r.b.Id).ToList();
        var reviewedBookingIds = await _db.MentorReviews.AsNoTracking()
            .Where(r => r.BookingId != null && bookingIds.Contains(r.BookingId!.Value))
            .Select(r => r.BookingId!.Value)
            .ToListAsync(ct);
        var reviewed = reviewedBookingIds.ToHashSet();

        IReadOnlyList<MentorBookingResponse> items = rows
            .Select(r => MentorBookingResponse.From(r.b, r.s, r.m, reviewed.Contains(r.b.Id)))
            .ToList();
        return Result.Success(items);
    }

    public async Task<Result<MentorBookingResponse>> SubmitReviewAsync(
        int bookingId, SubmitMentorReviewRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);

        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure<MentorBookingResponse>(Error.Unauthorized(
                "Booking.Unauthorized", "An authenticated user is required to review a session."));
        }

        // The StudentUserId predicate is the authorization check: a booking id belonging to someone
        // else resolves to nothing rather than letting this caller rate another student's mentor.
        var pair = await (from b in _db.MeetingBookings.AsNoTracking()
                          join s in _db.MentorSlots.AsNoTracking() on b.SlotId equals s.Id
                          join m in _db.Mentors on s.MentorId equals m.Id
                          where b.Id == bookingId && b.StudentUserId == userId
                          select new { b, s, m })
            .FirstOrDefaultAsync(ct);
        if (pair is null)
        {
            return Result.Failure<MentorBookingResponse>(Error.NotFound(
                "Booking.NotFound", $"Booking {bookingId} was not found among your sessions."));
        }

        if (!MeetingBookingStatus.Is(pair.b.Status, MeetingBookingStatus.Completed))
        {
            return Result.Failure<MentorBookingResponse>(Error.Validation(
                "Booking.NotCompleted",
                "Only a completed session can be reviewed. Your mentor marks the session complete " +
                "once it has taken place."));
        }

        if (await _db.MentorReviews.AnyAsync(r => r.BookingId == bookingId, ct))
        {
            return Result.Failure<MentorBookingResponse>(Error.Conflict(
                "Booking.AlreadyReviewed", "You have already reviewed this session."));
        }

        var profile = await _db.UserProfiles.AsNoTracking()
            .Where(u => u.Id == userId).Select(u => u.FullName).FirstOrDefaultAsync(ct);

        _db.MentorReviews.Add(new MentorReview
        {
            MentorId = pair.m.Id,
            BookingId = bookingId,
            StudentUserId = userId,
            // Denormalised at review time so the mentor's feedback list stays readable even if the
            // student later renames or deletes their profile.
            StudentName = !string.IsNullOrWhiteSpace(profile) ? profile : pair.b.StudentName,
            Rating = body.Rating,
            Comment = body.Comment?.Trim() ?? string.Empty,
        });

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Lost the race against the unique index on BookingId — the other write is the review.
            return Result.Failure<MentorBookingResponse>(Error.Conflict(
                "Booking.AlreadyReviewed", "You have already reviewed this session."));
        }

        await RefreshMentorRatingAsync(pair.m, ct);
        return Result.Success(MentorBookingResponse.From(pair.b, pair.s, pair.m, hasReview: true));
    }

    /// <summary>
    /// Recomputes the mentor's rating snapshot from the review rows.
    ///
    /// Derived rather than incrementally adjusted: an incremental average drifts as soon as a review
    /// is edited or removed, whereas recomputing is always correct and the row count here is small.
    /// </summary>
    private async Task RefreshMentorRatingAsync(Mentor mentor, CancellationToken ct)
    {
        var stats = await _db.MentorReviews.AsNoTracking()
            .Where(r => r.MentorId == mentor.Id)
            .GroupBy(_ => 1)
            .Select(g => new { Count = g.Count(), Average = g.Average(x => (decimal)x.Rating) })
            .FirstOrDefaultAsync(ct);

        mentor.TotalReviews = stats?.Count ?? 0;
        mentor.Rating = stats is null ? 0m : Math.Round(stats.Average, 2, MidpointRounding.AwayFromZero);
        await _db.SaveChangesAsync(ct);
    }

    // ── Admin slots ─────────────────────────────────────────────────────────

    public async Task<Result<IReadOnlyList<MentorSlotResponse>>> CreateSlotsAsync(
        CreateMentorSlotsRequest body, CancellationToken ct)
    {
        var starts = (body.StartTimes ?? new List<string>())
            .Select(s => DateTime.Parse(s, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal))
            .Distinct()
            .ToList();

        var mentorExists = await _db.Mentors.AnyAsync(m => m.Id == body.MentorId, ct);
        if (!mentorExists)
        {
            return Result.Failure<IReadOnlyList<MentorSlotResponse>>(Error.NotFound(
                "Mentor.NotFound", $"Mentor {body.MentorId} was not found."));
        }
        var existing = await _db.MentorSlots
            .Where(s => s.MentorId == body.MentorId && starts.Contains(s.StartTimeUtc))
            .Select(s => s.StartTimeUtc)
            .ToListAsync(ct);
        var existingSet = existing.ToHashSet();
        var created = new List<MentorSlot>();
        foreach (var start in starts)
        {
            if (existingSet.Contains(start)) continue;
            var slot = new MentorSlot
            {
                MentorId = body.MentorId,
                StartTimeUtc = start,
                EndTimeUtc = start.AddMinutes(DefaultSlotMinutes),
                IsBooked = false,
                MeetingLink = string.Empty,
                CreatedAt = DateTime.UtcNow,
            };
            _db.MentorSlots.Add(slot);
            created.Add(slot);
        }
        if (created.Count > 0) await _db.SaveChangesAsync(ct);
        IReadOnlyList<MentorSlotResponse> items = created.Select(MentorSlotResponse.From).ToList();
        return Result.Success(items);
    }

    public async Task<Result> DeleteSlotAsync(int id, CancellationToken ct)
    {
        var slot = await _db.MentorSlots.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (slot is null)
        {
            return Result.Failure(Error.NotFound("Slot.NotFound", $"Slot {id} was not found."));
        }
        if (slot.IsBooked)
        {
            return Result.Failure(Error.Validation(
                "Slot.Booked",
                "Cannot delete a booked slot — cancel the booking first."));
        }
        _db.MentorSlots.Remove(slot);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ── Admin bookings ──────────────────────────────────────────────────────

    public async Task<Result<IReadOnlyList<MentorBookingResponse>>> ListAdminBookingsAsync(CancellationToken ct)
    {
        var rows = await (from b in _db.MeetingBookings.AsNoTracking()
                          join s in _db.MentorSlots.AsNoTracking() on b.SlotId equals s.Id
                          join m in _db.Mentors.AsNoTracking() on s.MentorId equals m.Id
                          orderby s.StartTimeUtc descending
                          select new { b, s, m })
            .Take(PaginationRequest.MaxPageSize)
            .ToListAsync(ct);
        IReadOnlyList<MentorBookingResponse> items = rows
            .Select(r => MentorBookingResponse.From(r.b, r.s, r.m))
            .ToList();
        return Result.Success(items);
    }

    public async Task<Result> CancelBookingAsync(int id, CancellationToken ct)
    {
        var booking = await _db.MeetingBookings.FirstOrDefaultAsync(b => b.Id == id, ct);
        if (booking is null)
        {
            return Result.Failure(Error.NotFound("Booking.NotFound", $"Booking {id} was not found."));
        }
        if (MeetingBookingStatus.Is(booking.Status, MeetingBookingStatus.Cancelled))
        {
            return Result.Success();
        }
        booking.Status = MeetingBookingStatus.Cancelled;
        var slot = await _db.MentorSlots.FirstOrDefaultAsync(s => s.Id == booking.SlotId, ct);
        if (slot is not null) slot.IsBooked = false;
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ── Admin mentor lifecycle ──────────────────────────────────────────────

    public async Task<Result<MentorResponse>> GetMentorByIdAsync(int id, CancellationToken ct)
    {
        var mentor = await _db.Mentors.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, ct);
        if (mentor is null)
        {
            return Result.Failure<MentorResponse>(Error.NotFound(
                "Mentor.NotFound", $"Mentor {id} was not found."));
        }
        return Result.Success(MentorResponse.From(mentor));
    }

    public async Task<Result<MentorResponse>> OnboardAsync(OnboardMentorRequest body, CancellationToken ct)
    {
        var email = body.Email.Trim().ToLowerInvariant();
        var duplicate = await _db.Mentors.AnyAsync(m => m.Email == email, ct);
        if (duplicate)
        {
            return Result.Failure<MentorResponse>(Error.Validation(
                "Mentor.EmailExists", $"A mentor with email '{email}' already exists."));
        }
        var expertise = body.Expertise is null
            ? string.Empty
            : string.Join(", ", body.Expertise.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()));
        var mentor = new Mentor
        {
            Name = body.Name.Trim(),
            Email = email,
            Role = body.Role.Trim(),
            Company = body.Company.Trim(),
            YearsOfExperience = body.YearsOfExperience.ToString(CultureInfo.InvariantCulture),
            Expertise = expertise,
            PricePerSession = body.HourlyRateInr ?? 0,
            Bio = body.Bio?.Trim() ?? string.Empty,
            AvatarUrl = body.AvatarUrl?.Trim() ?? string.Empty,
            VerificationStatus = "Pending",
            IsActive = true,
        };
        _db.Mentors.Add(mentor);
        await _db.SaveChangesAsync(ct);
        return Result.Success(MentorResponse.From(mentor));
    }

    public async Task<Result<MentorResponse>> UpdateAsync(UpdateMentorRequest body, CancellationToken ct)
    {
        var mentor = await _db.Mentors.FirstOrDefaultAsync(m => m.Id == body.Id, ct);
        if (mentor is null)
        {
            return Result.Failure<MentorResponse>(Error.NotFound(
                "Mentor.NotFound", $"Mentor {body.Id} was not found."));
        }
        if (body.Status is not null) mentor.VerificationStatus = body.Status;
        if (body.IsActive is not null) mentor.IsActive = body.IsActive.Value;
        if (body.Name is not null) mentor.Name = body.Name.Trim();
        if (body.Role is not null) mentor.Role = body.Role.Trim();
        if (body.Company is not null) mentor.Company = body.Company.Trim();
        if (body.YearsOfExperience is not null)
            mentor.YearsOfExperience = body.YearsOfExperience.Value.ToString(CultureInfo.InvariantCulture);
        if (body.Expertise is not null)
        {
            mentor.Expertise = string.Join(", ",
                body.Expertise.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()));
        }
        if (body.HourlyRateInr is not null) mentor.PricePerSession = body.HourlyRateInr.Value;
        if (body.Bio is not null) mentor.Bio = body.Bio.Trim();
        if (body.AvatarUrl is not null) mentor.AvatarUrl = body.AvatarUrl.Trim();
        await _db.SaveChangesAsync(ct);
        return Result.Success(MentorResponse.From(mentor));
    }
}
