using System.Text.Json;
using CareerPlatform.Api.Features.LearningPaths.Domain;
using CareerPlatform.Api.Features.LearningPaths.Dto;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.LearningPaths.Service;

/// <summary>Learning-paths workflow. Ports the 5 legacy MediatR handlers verbatim.</summary>
internal sealed class LearningPathService : ILearningPathService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AppDbContext _db;
    public LearningPathService(AppDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<LearningPathResponse>>> ListAsync(
        string? targetRole, bool publishedOnly, CancellationToken ct)
    {
        var query = _db.LearningPaths.AsNoTracking();
        if (publishedOnly) query = query.Where(p => p.IsPublished);
        if (!string.IsNullOrWhiteSpace(targetRole))
        {
            var role = targetRole.Trim();
            query = query.Where(p => p.TargetRole == role);
        }
        var rows = await query
            .OrderByDescending(p => p.IsPopular).ThenBy(p => p.TargetRole).ThenBy(p => p.Title)
            .ToListAsync(ct);
        IReadOnlyList<LearningPathResponse> items = rows.Select(LearningPathResponse.From).ToList();
        return Result.Success(items);
    }

    public async Task<Result<LearningPathResponse>> GetAsync(string slug, CancellationToken ct)
    {
        var s = slug.Trim();
        var p = await _db.LearningPaths.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == s && x.IsPublished, ct);
        if (p is null)
        {
            return Result.Failure<LearningPathResponse>(Error.NotFound(
                "LearningPath.NotFound", $"Learning path '{s}' was not found."));
        }
        return Result.Success(LearningPathResponse.From(p));
    }

    public async Task<Result<LearningPathResponse>> CreateAsync(CreateLearningPathRequest r, CancellationToken ct)
    {
        var slug = r.Slug.Trim();
        var dup = await _db.LearningPaths.AnyAsync(p => p.Slug == slug, ct);
        if (dup)
        {
            return Result.Failure<LearningPathResponse>(Error.Validation(
                "LearningPath.SlugExists", $"A learning path with slug '{slug}' already exists."));
        }
        var p = new LearningPath
        {
            Slug = slug,
            Title = r.Title.Trim(),
            Description = r.Description?.Trim() ?? string.Empty,
            TargetRole = r.TargetRole.Trim(),
            EstimatedMonths = r.EstimatedMonths,
            MilestonesJson = JsonSerializer.Serialize(r.Milestones ?? Array.Empty<LearningPathMilestoneDto>(), JsonOptions),
            IsPopular = r.IsPopular,
            IsPublished = r.IsPublished,
        };
        _db.LearningPaths.Add(p);
        await _db.SaveChangesAsync(ct);
        return Result.Success(LearningPathResponse.From(p));
    }

    public async Task<Result<LearningPathResponse>> UpdateAsync(int id, UpdateLearningPathRequest r, CancellationToken ct)
    {
        var p = await _db.LearningPaths.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null)
        {
            return Result.Failure<LearningPathResponse>(Error.NotFound(
                "LearningPath.NotFound", $"Learning path {id} was not found."));
        }
        if (r.Slug is not null)
        {
            var slug = r.Slug.Trim();
            if (slug != p.Slug)
            {
                var dup = await _db.LearningPaths.AnyAsync(x => x.Slug == slug && x.Id != id, ct);
                if (dup)
                {
                    return Result.Failure<LearningPathResponse>(Error.Validation(
                        "LearningPath.SlugExists",
                        $"A different learning path already uses slug '{slug}'."));
                }
                p.Slug = slug;
            }
        }
        if (r.Title is not null) p.Title = r.Title.Trim();
        if (r.Description is not null) p.Description = r.Description;
        if (r.TargetRole is not null) p.TargetRole = r.TargetRole.Trim();
        if (r.EstimatedMonths is not null) p.EstimatedMonths = r.EstimatedMonths.Value;
        if (r.Milestones is not null)
            p.MilestonesJson = JsonSerializer.Serialize(r.Milestones, JsonOptions);
        if (r.IsPopular is not null) p.IsPopular = r.IsPopular.Value;
        if (r.IsPublished is not null) p.IsPublished = r.IsPublished.Value;
        await _db.SaveChangesAsync(ct);
        return Result.Success(LearningPathResponse.From(p));
    }

    public async Task<Result<LearningPathResponse>> GetByIdAsync(int id, CancellationToken ct)
    {
        var p = await _db.LearningPaths.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null)
        {
            return Result.Failure<LearningPathResponse>(Error.NotFound(
                "LearningPath.NotFound", $"Learning path {id} was not found."));
        }
        return Result.Success(LearningPathResponse.From(p));
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var p = await _db.LearningPaths.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null)
        {
            return Result.Failure(Error.NotFound(
                "LearningPath.NotFound", $"Learning path {id} was not found."));
        }
        _db.LearningPaths.Remove(p);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
