using CareerPlatform.Api.Features.StudentProfile.Domain;
using CareerPlatform.Api.Features.StudentProfile.Dto;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.StudentProfile.Service;

internal sealed class StudentProfileService : IStudentProfileService
{
    /// <summary>Guards against a client turning the profile into an unbounded list.</summary>
    private const int MaxEducationRows = 20;

    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public StudentProfileService(AppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    // ── Education ───────────────────────────────────────────────────────────

    public async Task<Result<EducationListResponse>> ListMyEducationAsync(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Result.Failure<EducationListResponse>(Unauthorized());
        }

        var rows = await MyEducation(userId).AsNoTracking()
            .OrderBy(e => e.DisplayOrder)
            .ThenByDescending(e => e.EndYear ?? int.MaxValue)
            .ThenByDescending(e => e.StartYear)
            .ToListAsync(ct);

        return Result.Success(new EducationListResponse(
            rows.Select(EducationResponse.From).ToList(),
            GradeScales.All.ToList()));
    }

    public async Task<Result<EducationResponse>> AddMyEducationAsync(
        UpsertEducationRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!TryGetUserId(out var userId))
        {
            return Result.Failure<EducationResponse>(Unauthorized());
        }

        var existingCount = await MyEducation(userId).CountAsync(ct);
        if (existingCount >= MaxEducationRows)
        {
            return Result.Failure<EducationResponse>(Error.Validation(
                "Education.TooMany",
                $"A profile may list at most {MaxEducationRows} qualifications."));
        }

        var entity = new StudentEducation { UserId = userId };
        var applied = Apply(request, entity);
        if (applied.IsFailure)
        {
            return Result.Failure<EducationResponse>(applied.Error);
        }

        _db.StudentEducations.Add(entity);
        await _db.SaveChangesAsync(ct);

        return Result.Success(EducationResponse.From(entity));
    }

    public async Task<Result<EducationResponse>> UpdateMyEducationAsync(
        int id, UpsertEducationRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!TryGetUserId(out var userId))
        {
            return Result.Failure<EducationResponse>(Unauthorized());
        }

        // Ownership is part of the predicate, so another student's row reads as "not found" rather
        // than leaking its existence through a 403.
        var entity = await MyEducation(userId).FirstOrDefaultAsync(e => e.Id == id, ct);
        if (entity is null)
        {
            return Result.Failure<EducationResponse>(Error.NotFound(
                "Education.NotFound", $"Qualification {id} was not found on your profile."));
        }

        var applied = Apply(request, entity);
        if (applied.IsFailure)
        {
            return Result.Failure<EducationResponse>(applied.Error);
        }

        await _db.SaveChangesAsync(ct);
        return Result.Success(EducationResponse.From(entity));
    }

    public async Task<Result> DeleteMyEducationAsync(int id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Result.Failure(Unauthorized());
        }

        var entity = await MyEducation(userId).FirstOrDefaultAsync(e => e.Id == id, ct);
        if (entity is null)
        {
            return Result.Failure(Error.NotFound(
                "Education.NotFound", $"Qualification {id} was not found on your profile."));
        }

        _db.StudentEducations.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ── Preferences ─────────────────────────────────────────────────────────

    public async Task<Result<PreferencesResponse>> GetMyPreferencesAsync(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
        {
            return Result.Failure<PreferencesResponse>(Unauthorized());
        }

        var row = await _db.StudentPreferences.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        // A student who has never opened settings has no row. Return the documented defaults from
        // the domain factory rather than 404-ing or letting the client invent them.
        return Result.Success(PreferencesResponse.From(
            row ?? StudentPreferences.CreateDefault(userId)));
    }

    public async Task<Result<PreferencesResponse>> UpdateMyPreferencesAsync(
        UpdatePreferencesRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!TryGetUserId(out var userId))
        {
            return Result.Failure<PreferencesResponse>(Unauthorized());
        }

        var row = await _db.StudentPreferences.FirstOrDefaultAsync(p => p.UserId == userId, ct);
        if (row is null)
        {
            row = StudentPreferences.CreateDefault(userId);
            _db.StudentPreferences.Add(row);
        }

        row.EmailNotificationsEnabled = request.EmailNotificationsEnabled;
        row.RecruiterVisibility = request.RecruiterVisibility;
        row.MentorshipRemindersEnabled = request.MentorshipRemindersEnabled;
        row.PromotionalEmailsEnabled = request.PromotionalEmailsEnabled;
        row.PreferredRole = Trimmed(request.PreferredRole);
        row.PreferredLocations = JoinLocations(request.PreferredLocations);

        await _db.SaveChangesAsync(ct);
        return Result.Success(PreferencesResponse.From(row));
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private IQueryable<StudentEducation> MyEducation(string userId) =>
        _db.StudentEducations.Where(e => e.UserId == userId);

    private bool TryGetUserId(out string userId)
    {
        userId = _currentUser.UserId ?? string.Empty;
        return userId.Length > 0;
    }

    private static Error Unauthorized() => Error.Unauthorized(
        "StudentProfile.Unauthorized", "An authenticated student is required.");

    /// <summary>
    /// Copies a request onto an entity, enforcing the cross-field rules FluentValidation cannot own
    /// cleanly (grade needs its scale; an ongoing course has no end year; years must not invert).
    /// </summary>
    private static Result Apply(UpsertEducationRequest request, StudentEducation entity)
    {
        if (request.GradeValue is not null && !GradeScales.IsSupported(request.GradeScale))
        {
            return Result.Failure(Error.Validation(
                "Education.GradeScaleRequired",
                $"A grade value needs a scale. Supported scales: {string.Join(", ", GradeScales.All)}."));
        }

        var maximum = GradeScales.MaximumFor(request.GradeScale);
        if (request.GradeValue is not null && maximum is not null
            && (request.GradeValue < 0 || request.GradeValue > maximum))
        {
            return Result.Failure(Error.Validation(
                "Education.GradeOutOfRange",
                $"A '{request.GradeScale}' grade must be between 0 and {maximum}."));
        }

        var endYear = request.IsCurrent ? null : request.EndYear;
        if (endYear is not null && endYear < request.StartYear)
        {
            return Result.Failure(Error.Validation(
                "Education.YearsInverted", "The end year cannot precede the start year."));
        }

        if (!request.IsCurrent && request.EndYear is null)
        {
            return Result.Failure(Error.Validation(
                "Education.EndYearRequired",
                "Provide an end year, or mark the qualification as currently ongoing."));
        }

        entity.Degree = request.Degree.Trim();
        entity.Institution = request.Institution.Trim();
        entity.FieldOfStudy = Trimmed(request.FieldOfStudy);
        entity.StartYear = request.StartYear;
        entity.EndYear = endYear;
        entity.IsCurrent = request.IsCurrent;
        entity.GradeValue = request.GradeValue;
        entity.GradeScale = request.GradeValue is null ? null : request.GradeScale;
        entity.Description = Trimmed(request.Description);
        entity.DisplayOrder = request.DisplayOrder;

        return Result.Success();
    }

    private static string? Trimmed(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? JoinLocations(IReadOnlyList<string>? locations)
    {
        if (locations is null || locations.Count == 0) return null;
        var cleaned = locations
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return cleaned.Count == 0 ? null : string.Join(", ", cleaned);
    }
}
