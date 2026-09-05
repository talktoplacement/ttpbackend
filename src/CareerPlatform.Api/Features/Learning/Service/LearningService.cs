using CareerPlatform.Api.Features.Learning.Domain;
using CareerPlatform.Api.Features.Learning.Dto;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.Learning.Service;

/// <summary>My-Learning workflow. Ports the 3 legacy MediatR handlers verbatim.</summary>
internal sealed class LearningService : ILearningService
{
    /// <summary>
    /// Resource kinds the polymorphic progress table accepts. `resourceType` arrives as a ROUTE
    /// parameter (not a body field), so the FluentValidation action filter never sees it — this
    /// allow-list is the only thing preventing arbitrary discriminator values from being written.
    /// Keep in sync with `LearningResourceType` in
    /// frontend/features/learning/services/learning-progress.service.ts.
    /// </summary>
    private static readonly HashSet<string> AllowedResourceTypes =
        new(StringComparer.OrdinalIgnoreCase) { "Course", "LearningPath", "Topic", "Lesson" };

    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public LearningService(AppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<LearningProgressResponse>>> ListMineAsync(
        string? resourceType, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure<IReadOnlyList<LearningProgressResponse>>(Error.Unauthorized(
                "Learning.Unauthorized", "An authenticated user is required."));
        }

        var query = _db.LearningProgress.AsNoTracking().Where(p => p.UserId == userId);
        if (!string.IsNullOrWhiteSpace(resourceType))
        {
            var t = resourceType.Trim();
            query = query.Where(p => p.ResourceType == t);
        }

        var rows = await query.OrderByDescending(p => p.LastAccessedAtUtc).ToListAsync(ct);
        IReadOnlyList<LearningProgressResponse> items =
            rows.Select(LearningProgressResponse.From).ToList();
        return Result.Success(items);
    }

    public async Task<Result<LearningProgressSummary>> GetSummaryAsync(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure<LearningProgressSummary>(Error.Unauthorized(
                "Learning.Unauthorized", "An authenticated user is required."));
        }

        var rows = await _db.LearningProgress.AsNoTracking()
            .Where(p => p.UserId == userId)
            .ToListAsync(ct);

        var byType = rows
            .GroupBy(r => r.ResourceType)
            .Select(g => new LearningProgressByType(
                g.Key,
                g.Count(),
                g.Count(r => r.Status == "in-progress"),
                g.Count(r => r.Status == "completed"),
                g.Any() ? (int)Math.Round(g.Average(r => r.PercentComplete)) : 0))
            .OrderBy(t => t.ResourceType)
            .ToList();

        var summary = new LearningProgressSummary(
            rows.Count,
            rows.Count(r => r.Status == "in-progress"),
            rows.Count(r => r.Status == "completed"),
            rows.Count > 0 ? (int)Math.Round(rows.Average(r => r.PercentComplete)) : 0,
            byType);

        return Result.Success(summary);
    }

    public async Task<Result<LearningProgressResponse>> UpsertAsync(
        string resourceType, int resourceId, UpsertProgressRequest request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure<LearningProgressResponse>(Error.Unauthorized(
                "Learning.Unauthorized", "An authenticated user is required."));
        }

        var type = resourceType.Trim();
        if (!AllowedResourceTypes.Contains(type))
        {
            return Result.Failure<LearningProgressResponse>(Error.Validation(
                "Learning.InvalidResourceType",
                $"ResourceType must be one of: {string.Join(", ", AllowedResourceTypes)}."));
        }
        var percent = Math.Clamp(request.PercentComplete, 0, 100);
        var derivedStatus = !string.IsNullOrWhiteSpace(request.Status)
            ? request.Status.Trim().ToLowerInvariant()
            : percent >= 100 ? "completed" : percent > 0 ? "in-progress" : "not-started";

        var row = await _db.LearningProgress
            .FirstOrDefaultAsync(
                p => p.UserId == userId && p.ResourceType == type && p.ResourceId == resourceId, ct);
        var now = DateTime.UtcNow;

        if (row is null)
        {
            row = new LearningProgress
            {
                UserId = userId,
                ResourceType = type,
                ResourceId = resourceId,
                PercentComplete = percent,
                Status = derivedStatus,
                LastAccessedAtUtc = now,
                CompletedAtUtc = derivedStatus == "completed" ? now : null,
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            };
            _db.LearningProgress.Add(row);
        }
        else
        {
            row.PercentComplete = percent;
            row.LastAccessedAtUtc = now;
            if (row.Status != "completed" && derivedStatus == "completed")
            {
                row.CompletedAtUtc = now;
            }
            else if (derivedStatus != "completed")
            {
                row.CompletedAtUtc = null;
            }
            row.Status = derivedStatus;
            if (request.Notes is not null)
            {
                row.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
            }
        }

        await _db.SaveChangesAsync(ct);
        return Result.Success(LearningProgressResponse.From(row));
    }
}
