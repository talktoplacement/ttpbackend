using CareerPlatform.Api.Features.Practice.Domain;
using CareerPlatform.Api.Features.Practice.Dto;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.Practice.Service;

internal sealed class PracticeService : IPracticeService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;
    public PracticeService(AppDbContext db, ICurrentUser currentUser) { _db = db; _currentUser = currentUser; }

    public async Task<Result<IReadOnlyList<PracticeQuestionResponse>>> ListAsync(string? category, bool publishedOnly, CancellationToken ct)
    {
        var q = _db.PracticeQuestions.AsNoTracking();
        if (publishedOnly) q = q.Where(x => x.IsPublished);
        if (!string.IsNullOrWhiteSpace(category))
        {
            var c = category.Trim();
            q = q.Where(x => x.Category == c);
        }
        var rows = await q.OrderBy(x => x.Difficulty).ThenBy(x => x.Title)
            .Take(PaginationRequest.MaxPageSize).ToListAsync(ct);
        IReadOnlyList<PracticeQuestionResponse> items = rows.Select(PracticeQuestionResponse.From).ToList();
        return Result.Success(items);
    }

    public async Task<Result<PracticeQuestionResponse>> GetAsync(string slug, CancellationToken ct)
    {
        var s = slug.Trim();
        var q = await _db.PracticeQuestions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == s && x.IsPublished, ct);
        if (q is null) return Result.Failure<PracticeQuestionResponse>(Error.NotFound(
            "Practice.NotFound", $"Practice question '{s}' was not found."));
        return Result.Success(PracticeQuestionResponse.From(q));
    }

    public async Task<Result<PracticeQuestionResponse>> CreateAsync(CreatePracticeQuestionRequest r, CancellationToken ct)
    {
        var slug = r.Slug.Trim();
        if (await _db.PracticeQuestions.AnyAsync(x => x.Slug == slug, ct))
        {
            return Result.Failure<PracticeQuestionResponse>(Error.Validation(
                "Practice.SlugExists", $"A practice question with slug '{slug}' already exists."));
        }
        var tags = r.CompanyTags is null ? string.Empty :
            string.Join(", ", r.CompanyTags.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
        var q = new PracticeQuestion
        {
            Slug = slug, Title = r.Title.Trim(),
            Description = r.Description?.Trim() ?? string.Empty,
            Difficulty = r.Difficulty, Category = r.Category.Trim(),
            AcceptanceRate = r.AcceptanceRate, CompanyTags = tags, IsPublished = r.IsPublished,
        };
        _db.PracticeQuestions.Add(q);
        await _db.SaveChangesAsync(ct);
        return Result.Success(PracticeQuestionResponse.From(q));
    }

    public async Task<Result<PracticeQuestionResponse>> UpdateAsync(int id, UpdatePracticeQuestionRequest r, CancellationToken ct)
    {
        var q = await _db.PracticeQuestions.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (q is null) return Result.Failure<PracticeQuestionResponse>(Error.NotFound(
            "Practice.NotFound", $"Practice question {id} was not found."));

        if (r.Slug is not null)
        {
            var slug = r.Slug.Trim();
            if (slug != q.Slug)
            {
                if (await _db.PracticeQuestions.AnyAsync(x => x.Slug == slug && x.Id != id, ct))
                    return Result.Failure<PracticeQuestionResponse>(Error.Validation(
                        "Practice.SlugExists", $"A different question already uses slug '{slug}'."));
                q.Slug = slug;
            }
        }
        if (r.Title is not null) q.Title = r.Title.Trim();
        if (r.Description is not null) q.Description = r.Description;
        if (r.Difficulty is not null) q.Difficulty = r.Difficulty;
        if (r.Category is not null) q.Category = r.Category.Trim();
        if (r.AcceptanceRate is not null) q.AcceptanceRate = r.AcceptanceRate.Value;
        if (r.CompanyTags is not null)
            q.CompanyTags = string.Join(", ", r.CompanyTags.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
        if (r.IsPublished is not null) q.IsPublished = r.IsPublished.Value;
        await _db.SaveChangesAsync(ct);
        return Result.Success(PracticeQuestionResponse.From(q));
    }

    public async Task<Result<PracticeQuestionResponse>> GetByIdAsync(int id, CancellationToken ct)
    {
        var q = await _db.PracticeQuestions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (q is null) return Result.Failure<PracticeQuestionResponse>(Error.NotFound(
            "Practice.NotFound", $"Practice question {id} was not found."));
        return Result.Success(PracticeQuestionResponse.From(q));
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var q = await _db.PracticeQuestions.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (q is null) return Result.Failure(Error.NotFound("Practice.NotFound", $"Practice question {id} was not found."));
        _db.PracticeQuestions.Remove(q);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<PracticeBookmarkResponse>>> ListMyBookmarksAsync(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
            return Result.Failure<IReadOnlyList<PracticeBookmarkResponse>>(Error.Unauthorized(
                "Practice.Unauthorized", "An authenticated user is required."));
        var rows = await _db.PracticeBookmarks.AsNoTracking()
            .Include(b => b.PracticeQuestion)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAtUtc).ToListAsync(ct);
        IReadOnlyList<PracticeBookmarkResponse> items = rows.Select(PracticeBookmarkResponse.From).ToList();
        return Result.Success(items);
    }

    public async Task<Result<PracticeBookmarkResponse>> AddBookmarkAsync(int questionId, string? notes, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
            return Result.Failure<PracticeBookmarkResponse>(Error.Unauthorized(
                "Practice.Unauthorized", "An authenticated user is required."));
        var q = await _db.PracticeQuestions.FirstOrDefaultAsync(x => x.Id == questionId, ct);
        if (q is null) return Result.Failure<PracticeBookmarkResponse>(Error.NotFound(
            "Practice.NotFound", $"Practice question {questionId} was not found."));
        var existing = await _db.PracticeBookmarks
            .FirstOrDefaultAsync(b => b.UserId == userId && b.PracticeQuestionId == questionId, ct);
        if (existing is null)
        {
            existing = new PracticeBookmark
            {
                UserId = userId, PracticeQuestionId = questionId,
                Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            };
            _db.PracticeBookmarks.Add(existing);
            await _db.SaveChangesAsync(ct);
        }
        existing.PracticeQuestion = q;
        return Result.Success(PracticeBookmarkResponse.From(existing));
    }

    public async Task<Result> RemoveBookmarkAsync(int questionId, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
            return Result.Failure(Error.Unauthorized("Practice.Unauthorized", "An authenticated user is required."));
        var existing = await _db.PracticeBookmarks
            .FirstOrDefaultAsync(b => b.UserId == userId && b.PracticeQuestionId == questionId, ct);
        if (existing is null) return Result.Success();
        _db.PracticeBookmarks.Remove(existing);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
