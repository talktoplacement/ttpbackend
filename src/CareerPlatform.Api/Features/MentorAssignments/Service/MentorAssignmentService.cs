using CareerPlatform.Api.Features.MentorAssignments.Domain;
using CareerPlatform.Api.Features.MentorAssignments.Dto;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.MentorAssignments.Service;

internal sealed class MentorAssignmentService : IMentorAssignmentService
{
    private const string StudentRole = "Student";

    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public MentorAssignmentService(AppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<MentorAssignmentResponse>>> ListAsync(
        bool activeOnly, CancellationToken ct)
    {
        var q = _db.MentorAssignments.AsNoTracking();
        if (activeOnly) q = q.Where(a => a.EndedAtUtc == null);

        // Left-join student + mentor so the admin table shows names, not raw ids. Both joins are
        // optional (DefaultIfEmpty) because a user or mentor row could be removed later.
        // Projected into a flat row first — DateTime? formatting can't be expressed in an EF
        // expression tree without tripping nullable analysis, so it happens in memory below.
        var flat = await (
            from a in q
            join u in _db.UserProfiles.AsNoTracking() on a.StudentUserId equals u.Id into us
            from u in us.DefaultIfEmpty()
            join m in _db.Mentors.AsNoTracking() on a.MentorId equals m.Id into ms
            from m in ms.DefaultIfEmpty()
            orderby a.AssignedAtUtc descending
            select new AssignmentJoinRow(
                a.Id, a.StudentUserId,
                u != null ? u.FullName : null,
                u != null ? u.Email : null,
                a.MentorId,
                m != null ? m.Name : null,
                m != null ? m.Company : null,
                a.CohortName, a.AssignedAtUtc, a.EndedAtUtc, a.Notes)
        ).Take(PaginationRequest.MaxPageSize).ToListAsync(ct);

        IReadOnlyList<MentorAssignmentResponse> rows = flat.Select(ToResponse).ToList();
        return Result.Success(rows);
    }

    public async Task<Result<MyMentorResponse?>> GetMyMentorAsync(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure<MyMentorResponse?>(Error.Unauthorized(
                "MentorAssignment.Unauthorized", "An authenticated student is required."));
        }

        // Inner join on Mentors: an assignment whose mentor row is gone cannot render a profile, so it
        // is treated as "no mentor" rather than returning a half-populated card.
        var row = await (
            from a in _db.MentorAssignments.AsNoTracking()
            where a.StudentUserId == userId && a.EndedAtUtc == null
            join m in _db.Mentors.AsNoTracking() on a.MentorId equals m.Id
            orderby a.AssignedAtUtc descending
            select new
            {
                a.Id,
                a.CohortName,
                a.AssignedAtUtc,
                MentorId = m.Id,
                m.Name,
                m.Email,
                m.Company,
                m.Role,
                m.AvatarUrl,
                m.Bio,
                m.Expertise,
            }).FirstOrDefaultAsync(ct);

        if (row is null)
        {
            // Not an error: most students have no dedicated mentor until an admin assigns one.
            return Result.Success<MyMentorResponse?>(null);
        }

        return Result.Success<MyMentorResponse?>(new MyMentorResponse(
            row.Id,
            row.MentorId,
            row.Name,
            row.Email,
            row.Company,
            row.Role,
            string.IsNullOrWhiteSpace(row.AvatarUrl) ? null : row.AvatarUrl,
            row.Bio ?? string.Empty,
            SplitExpertise(row.Expertise),
            row.CohortName,
            row.AssignedAtUtc.ToString("O")));
    }

    /// <summary>Expertise is stored denormalised as a comma-separated column.</summary>
    private static IReadOnlyList<string> SplitExpertise(string? raw) =>
        string.IsNullOrWhiteSpace(raw)
            ? Array.Empty<string>()
            : raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public async Task<Result<IReadOnlyList<EligibleStudentResponse>>> ListEligibleStudentsAsync(
        CancellationToken ct)
    {
        // "Eligible" = a Student-role user with no currently-active assignment.
        var assignedStudentIds = await _db.MentorAssignments.AsNoTracking()
            .Where(a => a.EndedAtUtc == null)
            .Select(a => a.StudentUserId)
            .ToListAsync(ct);

        var rows = await _db.UserProfiles.AsNoTracking()
            .Where(u => u.Role == StudentRole && !assignedStudentIds.Contains(u.Id))
            .OrderBy(u => u.FullName)
            .Take(PaginationRequest.MaxPageSize)
            .Select(u => new EligibleStudentResponse(u.Id, u.FullName, u.Email, u.PlanName))
            .ToListAsync(ct);

        return Result.Success((IReadOnlyList<EligibleStudentResponse>)rows);
    }

    public async Task<Result<IReadOnlyList<MentorPoolEntryResponse>>> ListMentorPoolAsync(
        CancellationToken ct)
    {
        var mentors = await _db.Mentors.AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.Name)
            .Take(PaginationRequest.MaxPageSize)
            .ToListAsync(ct);

        if (mentors.Count == 0)
        {
            return Result.Success((IReadOnlyList<MentorPoolEntryResponse>)Array.Empty<MentorPoolEntryResponse>());
        }

        // One grouped query for active load rather than N+1 per mentor.
        var mentorIds = mentors.Select(m => m.Id).ToList();
        var loadByMentorId = await _db.MentorAssignments.AsNoTracking()
            .Where(a => a.EndedAtUtc == null && mentorIds.Contains(a.MentorId))
            .GroupBy(a => a.MentorId)
            .Select(g => new { MentorId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.MentorId, x => x.Count, ct);

        IReadOnlyList<MentorPoolEntryResponse> rows = mentors
            .Select(m => new MentorPoolEntryResponse(
                m.Id, m.Name, m.Company, m.Role, m.VerificationStatus,
                loadByMentorId.GetValueOrDefault(m.Id, 0)))
            .ToList();

        return Result.Success(rows);
    }

    public async Task<Result<MentorAssignmentResponse>> CreateAsync(
        CreateMentorAssignmentRequest r, CancellationToken ct)
    {
        var studentId = r.StudentUserId.Trim();

        var studentExists = await _db.UserProfiles.AsNoTracking()
            .AnyAsync(u => u.Id == studentId, ct);
        if (!studentExists)
        {
            return Result.Failure<MentorAssignmentResponse>(Error.Validation(
                "MentorAssignment.StudentNotFound", $"User '{studentId}' does not exist."));
        }

        var mentorExists = await _db.Mentors.AsNoTracking()
            .AnyAsync(m => m.Id == r.MentorId && m.IsActive, ct);
        if (!mentorExists)
        {
            return Result.Failure<MentorAssignmentResponse>(Error.Validation(
                "MentorAssignment.MentorNotFound",
                $"Mentor {r.MentorId} does not exist or is inactive."));
        }

        // BUSINESS RULE: one active assignment per student. Checked here so the caller receives a
        // 409 with a readable message rather than a raw unique-index violation from Postgres.
        var alreadyAssigned = await _db.MentorAssignments
            .AnyAsync(a => a.StudentUserId == studentId && a.EndedAtUtc == null, ct);
        if (alreadyAssigned)
        {
            return Result.Failure<MentorAssignmentResponse>(Error.Conflict(
                "MentorAssignment.AlreadyAssigned",
                "This student already has an active mentor. End the current assignment first."));
        }

        var assignment = new MentorAssignment
        {
            StudentUserId = studentId,
            MentorId = r.MentorId,
            CohortName = r.CohortName?.Trim(),
            Notes = r.Notes?.Trim(),
            AssignedAtUtc = DateTime.UtcNow,
        };
        _db.MentorAssignments.Add(assignment);
        await _db.SaveChangesAsync(ct);

        return await ProjectSingleAsync(assignment.Id, ct);
    }

    public async Task<Result<MentorAssignmentResponse>> UpdateAsync(
        int id, UpdateMentorAssignmentRequest r, CancellationToken ct)
    {
        var assignment = await _db.MentorAssignments.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (assignment is null)
        {
            return Result.Failure<MentorAssignmentResponse>(Error.NotFound(
                "MentorAssignment.NotFound", $"Assignment {id} was not found."));
        }
        assignment.CohortName = r.CohortName?.Trim();
        assignment.Notes = r.Notes?.Trim();
        await _db.SaveChangesAsync(ct);
        return await ProjectSingleAsync(id, ct);
    }

    public async Task<Result<MentorAssignmentResponse>> EndAsync(int id, CancellationToken ct)
    {
        var assignment = await _db.MentorAssignments.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (assignment is null)
        {
            return Result.Failure<MentorAssignmentResponse>(Error.NotFound(
                "MentorAssignment.NotFound", $"Assignment {id} was not found."));
        }
        if (assignment.EndedAtUtc is not null)
        {
            return Result.Failure<MentorAssignmentResponse>(Error.Validation(
                "MentorAssignment.AlreadyEnded", "This assignment has already ended."));
        }
        assignment.EndedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return await ProjectSingleAsync(id, ct);
    }

    /// <summary>
    /// Re-reads one assignment through the same name-enriched projection the list uses, so create /
    /// update / end all return an identically-shaped payload the client can drop into its table.
    /// </summary>
    private async Task<Result<MentorAssignmentResponse>> ProjectSingleAsync(int id, CancellationToken ct)
    {
        var flat = await (
            from a in _db.MentorAssignments.AsNoTracking().Where(x => x.Id == id)
            join u in _db.UserProfiles.AsNoTracking() on a.StudentUserId equals u.Id into us
            from u in us.DefaultIfEmpty()
            join m in _db.Mentors.AsNoTracking() on a.MentorId equals m.Id into ms
            from m in ms.DefaultIfEmpty()
            select new AssignmentJoinRow(
                a.Id, a.StudentUserId,
                u != null ? u.FullName : null,
                u != null ? u.Email : null,
                a.MentorId,
                m != null ? m.Name : null,
                m != null ? m.Company : null,
                a.CohortName, a.AssignedAtUtc, a.EndedAtUtc, a.Notes)
        ).FirstOrDefaultAsync(ct);

        if (flat is null)
        {
            return Result.Failure<MentorAssignmentResponse>(Error.NotFound(
                "MentorAssignment.NotFound", $"Assignment {id} was not found."));
        }
        return Result.Success(ToResponse(flat));
    }

    /// <summary>
    /// Flat DB projection. Exists so DateTime? → ISO-string formatting happens in memory rather
    /// than inside an EF expression tree (where nullable narrowing isn't provable).
    /// </summary>
    private sealed record AssignmentJoinRow(
        int Id, string StudentUserId, string? StudentName, string? StudentEmail,
        int MentorId, string? MentorName, string? MentorCompany,
        string? CohortName, DateTime AssignedAtUtc, DateTime? EndedAtUtc, string? Notes);

    private static MentorAssignmentResponse ToResponse(AssignmentJoinRow r) =>
        new(r.Id, r.StudentUserId, r.StudentName, r.StudentEmail,
            r.MentorId, r.MentorName, r.MentorCompany, r.CohortName,
            r.AssignedAtUtc.ToString("O"),
            r.EndedAtUtc?.ToString("O"),
            r.Notes,
            r.EndedAtUtc is null);
}
