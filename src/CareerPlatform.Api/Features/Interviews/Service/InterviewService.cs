using CareerPlatform.Api.Features.Interviews.Domain;
using CareerPlatform.Api.Features.Interviews.Dto;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.Interviews.Service;

internal sealed class InterviewService : IInterviewService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;
    public InterviewService(AppDbContext db, ICurrentUser currentUser) { _db = db; _currentUser = currentUser; }

    // ---- Interview questions ----

    public async Task<Result<IReadOnlyList<InterviewQuestionResponse>>> ListQuestionsAsync(
        string? topic, string? difficulty, bool publishedOnly, CancellationToken ct)
    {
        var q = _db.InterviewQuestions.AsNoTracking();
        if (publishedOnly) q = q.Where(x => x.IsPublished);
        if (!string.IsNullOrWhiteSpace(topic))
        {
            var t = topic.Trim();
            q = q.Where(x => x.Topic == t);
        }
        if (!string.IsNullOrWhiteSpace(difficulty))
        {
            var d = difficulty.Trim();
            q = q.Where(x => x.Difficulty == d);
        }
        var rows = await q.OrderBy(x => x.Topic).ThenBy(x => x.Difficulty).ThenBy(x => x.Prompt)
            .Take(PaginationRequest.MaxPageSize).ToListAsync(ct);
        IReadOnlyList<InterviewQuestionResponse> items = rows.Select(InterviewQuestionResponse.From).ToList();
        return Result.Success(items);
    }

    public async Task<Result<InterviewQuestionResponse>> CreateQuestionAsync(CreateInterviewQuestionRequest r, CancellationToken ct)
    {
        var slug = r.Slug.Trim();
        var dup = await _db.InterviewQuestions.AnyAsync(x => x.Slug == slug, ct);
        if (dup)
        {
            return Result.Failure<InterviewQuestionResponse>(Error.Validation(
                "InterviewQuestion.SlugExists", $"An interview question with slug '{slug}' already exists."));
        }
        var tags = r.CompanyTags is null ? string.Empty :
            string.Join(", ", r.CompanyTags.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()));
        var q = new InterviewQuestion
        {
            Slug = slug,
            Prompt = r.Prompt.Trim(),
            ExpectedAnswer = r.ExpectedAnswer?.Trim() ?? string.Empty,
            Topic = r.Topic.Trim(),
            Difficulty = r.Difficulty,
            CompanyTags = tags,
            IsPublished = r.IsPublished,
        };
        _db.InterviewQuestions.Add(q);
        await _db.SaveChangesAsync(ct);
        return Result.Success(InterviewQuestionResponse.From(q));
    }

    public async Task<Result<InterviewQuestionResponse>> UpdateQuestionAsync(int id, UpdateInterviewQuestionRequest r, CancellationToken ct)
    {
        var q = await _db.InterviewQuestions.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (q is null)
        {
            return Result.Failure<InterviewQuestionResponse>(Error.NotFound(
                "InterviewQuestion.NotFound", $"Interview question {id} was not found."));
        }
        if (r.Slug is not null)
        {
            var slug = r.Slug.Trim();
            if (slug != q.Slug)
            {
                var dup = await _db.InterviewQuestions.AnyAsync(x => x.Slug == slug && x.Id != id, ct);
                if (dup)
                {
                    return Result.Failure<InterviewQuestionResponse>(Error.Validation(
                        "InterviewQuestion.SlugExists", $"A different question already uses slug '{slug}'."));
                }
                q.Slug = slug;
            }
        }
        if (r.Prompt is not null) q.Prompt = r.Prompt.Trim();
        if (r.ExpectedAnswer is not null) q.ExpectedAnswer = r.ExpectedAnswer;
        if (r.Topic is not null) q.Topic = r.Topic.Trim();
        if (r.Difficulty is not null) q.Difficulty = r.Difficulty;
        if (r.CompanyTags is not null)
        {
            q.CompanyTags = string.Join(", ",
                r.CompanyTags.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()));
        }
        if (r.IsPublished is not null) q.IsPublished = r.IsPublished.Value;
        await _db.SaveChangesAsync(ct);
        return Result.Success(InterviewQuestionResponse.From(q));
    }

    public async Task<Result<InterviewQuestionResponse>> GetQuestionByIdAsync(int id, CancellationToken ct)
    {
        var q = await _db.InterviewQuestions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (q is null) return Result.Failure<InterviewQuestionResponse>(Error.NotFound(
            "InterviewQuestion.NotFound", $"Interview question {id} was not found."));
        return Result.Success(InterviewQuestionResponse.From(q));
    }

    public async Task<Result> DeleteQuestionAsync(int id, CancellationToken ct)
    {
        var q = await _db.InterviewQuestions.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (q is null)
        {
            return Result.Failure(Error.NotFound(
                "InterviewQuestion.NotFound", $"Interview question {id} was not found."));
        }
        _db.InterviewQuestions.Remove(q);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ---- Mock interview sessions ----

    /// <summary>
    /// Groups the published question bank into topics and joins the caller's own session history.
    ///
    /// Both halves are aggregated in the database rather than by loading every question, so the hub
    /// stays a fixed number of queries as the bank grows. Company tags are the one thing that must be
    /// split client-side of the database boundary, because they are stored denormalised as a
    /// comma-separated column.
    /// </summary>
    public async Task<Result<IReadOnlyList<InterviewTopicResponse>>> ListTopicsAsync(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure<IReadOnlyList<InterviewTopicResponse>>(Error.Unauthorized(
                "Interview.Unauthorized", "An authenticated user is required."));
        }

        var byTopicDifficulty = await _db.InterviewQuestions.AsNoTracking()
            .Where(q => q.IsPublished)
            .GroupBy(q => new { q.Topic, q.Difficulty })
            .Select(g => new { g.Key.Topic, g.Key.Difficulty, Count = g.Count() })
            .ToListAsync(ct);

        if (byTopicDifficulty.Count == 0)
        {
            return Result.Success<IReadOnlyList<InterviewTopicResponse>>(Array.Empty<InterviewTopicResponse>());
        }

        // Only the tag column is materialised, not whole question rows.
        var tagRows = await _db.InterviewQuestions.AsNoTracking()
            .Where(q => q.IsPublished && q.CompanyTags != "")
            .Select(q => new { q.Topic, q.CompanyTags })
            .ToListAsync(ct);

        var mySessions = await _db.MockInterviewSessions.AsNoTracking()
            .Where(s => s.UserId == userId)
            .GroupBy(s => s.Topic)
            .Select(g => new
            {
                Topic = g.Key,
                Total = g.Count(),
                Completed = g.Count(s => s.Status == "completed"),
                BestScore = g.Max(s => s.Score),
            })
            .ToListAsync(ct);

        var sessionsByTopic = mySessions.ToDictionary(s => s.Topic, StringComparer.OrdinalIgnoreCase);

        var tagsByTopic = tagRows
            .GroupBy(r => r.Topic, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.SelectMany(r => r.CompanyTags.Split(
                        ',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                StringComparer.OrdinalIgnoreCase);

        var topics = byTopicDifficulty
            .GroupBy(x => x.Topic, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                sessionsByTopic.TryGetValue(group.Key, out var mine);
                tagsByTopic.TryGetValue(group.Key, out var tags);

                return new InterviewTopicResponse(
                    group.Key,
                    group.Sum(x => x.Count),
                    group
                        .OrderBy(x => DifficultyRank(x.Difficulty))
                        .ThenBy(x => x.Difficulty, StringComparer.OrdinalIgnoreCase)
                        .Select(x => new DifficultyCount(x.Difficulty, x.Count))
                        .ToList(),
                    tags ?? new List<string>(),
                    mine?.Total ?? 0,
                    mine?.Completed ?? 0,
                    mine?.BestScore);
            })
            .OrderByDescending(t => t.QuestionCount)
            .ThenBy(t => t.Topic, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Result.Success<IReadOnlyList<InterviewTopicResponse>>(topics);
    }

    /// <summary>
    /// Sort key for the canonical difficulty ladder. Unrecognised labels sort last rather than being
    /// dropped, because <c>Difficulty</c> is free text on the question row.
    /// </summary>
    private static int DifficultyRank(string difficulty) => difficulty.ToLowerInvariant() switch
    {
        "easy" => 0,
        "medium" => 1,
        "hard" => 2,
        _ => 3,
    };

    public async Task<Result<IReadOnlyList<MockInterviewSessionResponse>>> ListMySessionsAsync(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure<IReadOnlyList<MockInterviewSessionResponse>>(Error.Unauthorized(
                "Interview.Unauthorized", "An authenticated user is required."));
        }
        var rows = await _db.MockInterviewSessions.AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAtUtc)
            .Take(PaginationRequest.MaxPageSize).ToListAsync(ct);
        IReadOnlyList<MockInterviewSessionResponse> items = rows.Select(MockInterviewSessionResponse.From).ToList();
        return Result.Success(items);
    }

    public async Task<Result<MockInterviewSessionResponse>> CreateMySessionAsync(CreateInterviewSessionRequest r, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure<MockInterviewSessionResponse>(Error.Unauthorized(
                "Interview.Unauthorized", "An authenticated user is required."));
        }
        var s = new MockInterviewSession
        {
            UserId = userId,
            Type = r.Type,
            Topic = r.Topic.Trim(),
            DurationMinutes = r.DurationMinutes,
            Status = "scheduled",
            RubricReportJson = "{}",
        };
        _db.MockInterviewSessions.Add(s);
        await _db.SaveChangesAsync(ct);
        return Result.Success(MockInterviewSessionResponse.From(s));
    }

    public async Task<Result<MockInterviewSessionResponse>> UpdateMySessionAsync(int id, UpdateInterviewSessionRequest r, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure<MockInterviewSessionResponse>(Error.Unauthorized(
                "Interview.Unauthorized", "An authenticated user is required."));
        }
        var s = await _db.MockInterviewSessions.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
        if (s is null)
        {
            return Result.Failure<MockInterviewSessionResponse>(Error.NotFound(
                "Interview.SessionNotFound", $"Session {id} was not found."));
        }
        if (r.Status is not null) s.Status = r.Status;
        if (r.Score is not null) s.Score = r.Score;
        if (r.RubricReportJson is not null) s.RubricReportJson = r.RubricReportJson;
        await _db.SaveChangesAsync(ct);
        return Result.Success(MockInterviewSessionResponse.From(s));
    }

    public async Task<Result<IReadOnlyList<AdminMockInterviewSessionResponse>>> ListAllSessionsAsync(
        string? status, string? topic, CancellationToken ct)
    {
        var q = _db.MockInterviewSessions.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status))
        {
            var s = status.Trim().ToLowerInvariant();
            q = q.Where(x => x.Status == s);
        }
        if (!string.IsNullOrWhiteSpace(topic))
        {
            var t = topic.Trim();
            q = q.Where(x => x.Topic == t);
        }
        var rows = await q
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(PaginationRequest.MaxPageSize).ToListAsync(ct);
        IReadOnlyList<AdminMockInterviewSessionResponse> items =
            rows.Select(AdminMockInterviewSessionResponse.From).ToList();
        return Result.Success(items);
    }
}
