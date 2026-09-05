using CareerPlatform.Api.Features.PlacementPlans.Domain;
using CareerPlatform.Api.Features.PlacementPlans.Dto;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.PlacementPlans.Service;

internal sealed class PlacementPlanService : IPlacementPlanService
{
    private readonly AppDbContext _db;
    public PlacementPlanService(AppDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<PlacementPlanResponse>>> ListAsync(bool publishedOnly, CancellationToken ct)
    {
        var query = _db.PlacementPlans.AsNoTracking();
        if (publishedOnly) query = query.Where(p => p.IsPublished);
        var rows = await query
            .OrderBy(p => p.Title)
            .Take(PaginationRequest.MaxPageSize)
            .ToListAsync(ct);
        return Result.Success((IReadOnlyList<PlacementPlanResponse>)rows.Select(PlacementPlanResponse.From).ToList());
    }

    public async Task<Result<PlacementPlanResponse>> GetByIdAsync(int id, CancellationToken ct)
    {
        var p = await _db.PlacementPlans.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null)
        {
            return Result.Failure<PlacementPlanResponse>(Error.NotFound(
                "PlacementPlan.NotFound", $"Placement plan {id} was not found."));
        }
        return Result.Success(PlacementPlanResponse.From(p));
    }

    public async Task<Result<PlacementPlanResponse>> CreateAsync(CreatePlacementPlanRequest r, CancellationToken ct)
    {
        var slug = r.Slug.Trim();
        if (await _db.PlacementPlans.AnyAsync(p => p.Slug == slug, ct))
        {
            return Result.Failure<PlacementPlanResponse>(Error.Validation(
                "PlacementPlan.SlugExists", $"A placement plan with slug '{slug}' already exists."));
        }
        var plan = new PlacementPlan
        {
            Slug = slug,
            Title = r.Title.Trim(),
            Description = r.Description?.Trim() ?? string.Empty,
            DurationWeeks = r.DurationWeeks,
            Price = r.Price,
            IsPublished = r.IsPublished,
        };
        _db.PlacementPlans.Add(plan);
        await _db.SaveChangesAsync(ct);
        return Result.Success(PlacementPlanResponse.From(plan));
    }

    public async Task<Result<PlacementPlanResponse>> UpdateAsync(int id, UpdatePlacementPlanRequest r, CancellationToken ct)
    {
        var plan = await _db.PlacementPlans.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (plan is null)
        {
            return Result.Failure<PlacementPlanResponse>(Error.NotFound(
                "PlacementPlan.NotFound", $"Placement plan {id} was not found."));
        }
        if (r.Slug is not null)
        {
            var slug = r.Slug.Trim();
            if (slug != plan.Slug)
            {
                if (await _db.PlacementPlans.AnyAsync(x => x.Slug == slug && x.Id != id, ct))
                {
                    return Result.Failure<PlacementPlanResponse>(Error.Validation(
                        "PlacementPlan.SlugExists", $"A different placement plan already uses slug '{slug}'."));
                }
                plan.Slug = slug;
            }
        }
        if (r.Title is not null) plan.Title = r.Title.Trim();
        if (r.Description is not null) plan.Description = r.Description.Trim();
        if (r.DurationWeeks is not null) plan.DurationWeeks = r.DurationWeeks.Value;
        if (r.Price is not null) plan.Price = r.Price.Value;
        if (r.IsPublished is not null) plan.IsPublished = r.IsPublished.Value;
        await _db.SaveChangesAsync(ct);
        return Result.Success(PlacementPlanResponse.From(plan));
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var plan = await _db.PlacementPlans.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (plan is null)
        {
            return Result.Failure(Error.NotFound(
                "PlacementPlan.NotFound", $"Placement plan {id} was not found."));
        }
        _db.PlacementPlans.Remove(plan);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
