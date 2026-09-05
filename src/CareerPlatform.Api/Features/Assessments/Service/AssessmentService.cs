using System.Globalization;
using System.Text.Json;
using CareerPlatform.Api.Features.Assessments.Domain;
using CareerPlatform.Api.Features.Assessments.Dto;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.Assessments.Service;

internal sealed class AssessmentService : IAssessmentService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;
    public AssessmentService(AppDbContext db, ICurrentUser currentUser) { _db = db; _currentUser = currentUser; }

    // ---- Assessment catalog ----

    public async Task<Result<IReadOnlyList<AssessmentResponse>>> ListAsync(string? category, bool publishedOnly, CancellationToken ct)
    {
        var q = _db.Assessments.AsNoTracking();
        if (publishedOnly) q = q.Where(a => a.IsPublished);
        if (!string.IsNullOrWhiteSpace(category))
        {
            var c = category.Trim();
            q = q.Where(a => a.Category == c);
        }
        var rows = await q.OrderBy(a => a.Category).ThenBy(a => a.Title)
            .Take(PaginationRequest.MaxPageSize).ToListAsync(ct);
        IReadOnlyList<AssessmentResponse> items = rows.Select(AssessmentResponse.From).ToList();
        return Result.Success(items);
    }

    public async Task<Result<AssessmentResponse>> GetAsync(string slug, CancellationToken ct)
    {
        var s = slug.Trim();
        var a = await _db.Assessments.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == s && x.IsPublished, ct);
        if (a is null) return Result.Failure<AssessmentResponse>(Error.NotFound(
            "Assessment.NotFound", $"Assessment '{s}' was not found."));
        return Result.Success(AssessmentResponse.From(a));
    }

    public async Task<Result<AssessmentResponse>> GetByIdAsync(int id, CancellationToken ct)
    {
        var a = await _db.Assessments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (a is null) return Result.Failure<AssessmentResponse>(Error.NotFound(
            "Assessment.NotFound", $"Assessment {id} was not found."));
        return Result.Success(AssessmentResponse.From(a));
    }

    public async Task<Result<AssessmentResponse>> CreateAsync(CreateAssessmentRequest r, CancellationToken ct)
    {
        var slug = r.Slug.Trim();
        if (await _db.Assessments.AnyAsync(x => x.Slug == slug, ct))
            return Result.Failure<AssessmentResponse>(Error.Validation(
                "Assessment.SlugExists", $"An assessment with slug '{slug}' already exists."));
        var a = new Assessment
        {
            Slug = slug, Title = r.Title.Trim(),
            Description = r.Description?.Trim() ?? string.Empty,
            DurationMinutes = r.DurationMinutes, TotalMarks = r.TotalMarks,
            PassingMarks = r.PassingMarks, QuestionsCount = r.QuestionsCount,
            Category = r.Category.Trim(),
            StartsAtUtc = ParseDate(r.StartsAtUtc), EndsAtUtc = ParseDate(r.EndsAtUtc),
            IsPublished = r.IsPublished,
        };
        _db.Assessments.Add(a);
        await _db.SaveChangesAsync(ct);
        return Result.Success(AssessmentResponse.From(a));
    }

    public async Task<Result<AssessmentResponse>> UpdateAsync(int id, UpdateAssessmentRequest r, CancellationToken ct)
    {
        var a = await _db.Assessments.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (a is null) return Result.Failure<AssessmentResponse>(Error.NotFound(
            "Assessment.NotFound", $"Assessment {id} was not found."));

        if (r.Slug is not null)
        {
            var slug = r.Slug.Trim();
            if (slug != a.Slug)
            {
                if (await _db.Assessments.AnyAsync(x => x.Slug == slug && x.Id != id, ct))
                    return Result.Failure<AssessmentResponse>(Error.Validation(
                        "Assessment.SlugExists", $"A different assessment already uses slug '{slug}'."));
                a.Slug = slug;
            }
        }
        if (r.Title is not null) a.Title = r.Title.Trim();
        if (r.Description is not null) a.Description = r.Description;
        if (r.DurationMinutes is not null) a.DurationMinutes = r.DurationMinutes.Value;
        if (r.TotalMarks is not null) a.TotalMarks = r.TotalMarks.Value;
        if (r.PassingMarks is not null) a.PassingMarks = r.PassingMarks.Value;
        if (r.QuestionsCount is not null) a.QuestionsCount = r.QuestionsCount.Value;
        if (r.Category is not null) a.Category = r.Category.Trim();
        if (r.StartsAtUtc is not null) a.StartsAtUtc = ParseDate(r.StartsAtUtc);
        if (r.EndsAtUtc is not null) a.EndsAtUtc = ParseDate(r.EndsAtUtc);
        if (r.IsPublished is not null) a.IsPublished = r.IsPublished.Value;
        await _db.SaveChangesAsync(ct);
        return Result.Success(AssessmentResponse.From(a));
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var a = await _db.Assessments.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (a is null) return Result.Failure(Error.NotFound(
            "Assessment.NotFound", $"Assessment {id} was not found."));
        _db.Assessments.Remove(a);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ---- Attempts ----

    public async Task<Result<IReadOnlyList<AssessmentAttemptResponse>>> ListMyAttemptsAsync(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
            return Result.Failure<IReadOnlyList<AssessmentAttemptResponse>>(Error.Unauthorized(
                "Assessment.Unauthorized", "An authenticated user is required."));
        var rows = await _db.AssessmentAttempts.AsNoTracking()
            .Include(a => a.Assessment)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.StartedAtUtc).ToListAsync(ct);
        IReadOnlyList<AssessmentAttemptResponse> items = rows.Select(a => AssessmentAttemptResponse.From(a, includeAnswers: false)).ToList();
        return Result.Success(items);
    }

    public async Task<Result<AssessmentAttemptResponse>> GetMyAttemptAsync(int id, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
            return Result.Failure<AssessmentAttemptResponse>(Error.Unauthorized(
                "Assessment.Unauthorized", "An authenticated user is required."));
        var a = await _db.AssessmentAttempts.AsNoTracking()
            .Include(x => x.Assessment)
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
        if (a is null) return Result.Failure<AssessmentAttemptResponse>(Error.NotFound(
            "Assessment.AttemptNotFound", $"Attempt {id} was not found."));
        return Result.Success(AssessmentAttemptResponse.From(a, includeAnswers: true));
    }

    public async Task<Result<AssessmentAttemptResponse>> StartAttemptAsync(string slug, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
            return Result.Failure<AssessmentAttemptResponse>(Error.Unauthorized(
                "Assessment.Unauthorized", "An authenticated user is required."));
        var s = slug.Trim();
        var assessment = await _db.Assessments.FirstOrDefaultAsync(x => x.Slug == s && x.IsPublished, ct);
        if (assessment is null)
            return Result.Failure<AssessmentAttemptResponse>(Error.NotFound(
                "Assessment.NotFound", $"Assessment '{s}' was not found."));

        var attempt = new AssessmentAttempt
        {
            AssessmentId = assessment.Id, UserId = userId,
            StartedAtUtc = DateTime.UtcNow, AnswersJson = "{}",
            TotalMarks = assessment.TotalMarks, PassingMarks = assessment.PassingMarks,
        };
        _db.AssessmentAttempts.Add(attempt);
        await _db.SaveChangesAsync(ct);
        attempt.Assessment = assessment;
        return Result.Success(AssessmentAttemptResponse.From(attempt, includeAnswers: false));
    }

    /// <summary>
    /// Parses an operator-supplied ISO-8601 window bound. Non-throwing on purpose: the request
    /// validator rejects unparseable input first, so reaching this with garbage means a caller
    /// bypassed validation and the safest interpretation is "no bound" rather than a 500.
    /// </summary>
    private static DateTime? ParseDate(string? s) =>
        DateTime.TryParse(s, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed)
            ? parsed
            : null;
}
