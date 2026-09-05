using CareerPlatform.Api.Features.MentorshipPlans.Domain;
using CareerPlatform.Api.Features.MentorshipPlans.Dto;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.MentorshipPlans.Service;

internal sealed class MentorshipPlanService : IMentorshipPlanService
{
    private readonly AppDbContext _db;
    public MentorshipPlanService(AppDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<MentorshipPlanResponse>>> ListAsync(bool publishedOnly, CancellationToken ct)
    {
        var query = _db.MentorshipPlans.AsNoTracking();
        if (publishedOnly) query = query.Where(p => p.IsPublished);
        var rows = await query
            .OrderBy(p => p.Price)
            .Take(PaginationRequest.MaxPageSize)
            .ToListAsync(ct);
        return Result.Success((IReadOnlyList<MentorshipPlanResponse>)rows.Select(MentorshipPlanResponse.From).ToList());
    }

    public async Task<Result<MentorshipPlanResponse>> GetByIdAsync(int id, CancellationToken ct)
    {
        var p = await _db.MentorshipPlans.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null)
        {
            return Result.Failure<MentorshipPlanResponse>(Error.NotFound(
                "MentorshipPlan.NotFound", $"Mentorship plan {id} was not found."));
        }
        return Result.Success(MentorshipPlanResponse.From(p));
    }

    public async Task<Result<MentorshipPlanResponse>> CreateAsync(CreateMentorshipPlanRequest r, CancellationToken ct)
    {
        var slug = r.Slug.Trim();
        if (await _db.MentorshipPlans.AnyAsync(p => p.Slug == slug, ct))
        {
            return Result.Failure<MentorshipPlanResponse>(Error.Validation(
                "MentorshipPlan.SlugExists", $"A mentorship plan with slug '{slug}' already exists."));
        }
        var plan = new MentorshipPlan
        {
            Slug = slug,
            Title = r.Title.Trim(),
            Description = r.Description?.Trim() ?? string.Empty,
            DurationMinutes = r.DurationMinutes,
            Price = r.Price,
            CommissionPercent = r.CommissionPercent,
            IsPublished = r.IsPublished,
        };
        _db.MentorshipPlans.Add(plan);
        await _db.SaveChangesAsync(ct);
        return Result.Success(MentorshipPlanResponse.From(plan));
    }

    public async Task<Result<MentorshipPlanResponse>> UpdateAsync(int id, UpdateMentorshipPlanRequest r, CancellationToken ct)
    {
        var plan = await _db.MentorshipPlans.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (plan is null)
        {
            return Result.Failure<MentorshipPlanResponse>(Error.NotFound(
                "MentorshipPlan.NotFound", $"Mentorship plan {id} was not found."));
        }
        if (r.Slug is not null)
        {
            var slug = r.Slug.Trim();
            if (slug != plan.Slug)
            {
                if (await _db.MentorshipPlans.AnyAsync(x => x.Slug == slug && x.Id != id, ct))
                {
                    return Result.Failure<MentorshipPlanResponse>(Error.Validation(
                        "MentorshipPlan.SlugExists", $"A different mentorship plan already uses slug '{slug}'."));
                }
                plan.Slug = slug;
            }
        }
        if (r.Title is not null) plan.Title = r.Title.Trim();
        if (r.Description is not null) plan.Description = r.Description.Trim();
        if (r.DurationMinutes is not null) plan.DurationMinutes = r.DurationMinutes.Value;
        if (r.Price is not null) plan.Price = r.Price.Value;
        if (r.CommissionPercent is not null) plan.CommissionPercent = r.CommissionPercent.Value;
        if (r.IsPublished is not null) plan.IsPublished = r.IsPublished.Value;
        await _db.SaveChangesAsync(ct);
        return Result.Success(MentorshipPlanResponse.From(plan));
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var plan = await _db.MentorshipPlans.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (plan is null)
        {
            return Result.Failure(Error.NotFound(
                "MentorshipPlan.NotFound", $"Mentorship plan {id} was not found."));
        }
        _db.MentorshipPlans.Remove(plan);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
