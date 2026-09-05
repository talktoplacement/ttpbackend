using CareerPlatform.Api.Configuration;
using CareerPlatform.Api.Features.SubscriptionPlans.Domain;
using CareerPlatform.Api.Features.SubscriptionPlans.Dto;
using CareerPlatform.Api.Features.SubscriptionPlans.Lifecycle;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.SubscriptionPlans.Service;

internal sealed class SubscriptionPlanService : ISubscriptionPlanService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly SubscriptionOptions _options;
    public SubscriptionPlanService(AppDbContext db, ICurrentUser currentUser, IOptions<SubscriptionOptions> options)
    { _db = db; _currentUser = currentUser; _options = options.Value; }

    public async Task<Result<PaginatedResult<PlanResponse>>> ListAsync(int? page, int? pageSize, CancellationToken ct)
    {
        var p = new PaginationRequest(page, pageSize);
        var effectivePage = p.EffectivePage;
        var effectiveSize = Math.Min(p.EffectivePageSize, _options.CatalogMaxPageSize);
        var total = await _db.SubscriptionPlans.LongCountAsync(ct);
        var rows = await _db.SubscriptionPlans.AsNoTracking()
            .OrderBy(x => x.Id)
            .Skip((effectivePage - 1) * effectiveSize).Take(effectiveSize)
            .ToListAsync(ct);
        IReadOnlyList<PlanResponse> items = rows.Select(PlanResponse.From).ToList();
        return Result.Success(PaginatedResult<PlanResponse>.Create(items, effectivePage, effectiveSize, total));
    }

    public async Task<Result<IReadOnlyList<CatalogPlanResponse>>> ListActiveAsync(CancellationToken ct)
    {
        var rows = await _db.SubscriptionPlans.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Price)
            .Take(_options.CatalogMaxPageSize).ToListAsync(ct);
        IReadOnlyList<CatalogPlanResponse> items = rows.Select(p => new CatalogPlanResponse(
            p.Id, p.Code, p.Name, p.Description, p.Price, p.Currency, p.IntervalUnit, p.IntervalCount)).ToList();
        return Result.Success(items);
    }

    public async Task<Result<PlanResponse>> GetAsync(int id, CancellationToken ct)
    {
        var p = await _db.SubscriptionPlans.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return Result.Failure<PlanResponse>(Error.NotFound(
            "Plan.NotFound", $"Plan {id} was not found."));
        return Result.Success(PlanResponse.From(p));
    }

    public async Task<Result<PlanResponse>> CreateAsync(CreatePlanRequest r, CancellationToken ct)
    {
        var code = r.Code.Trim();
        if (await _db.SubscriptionPlans.AnyAsync(x => x.Code == code, ct))
            return Result.Failure<PlanResponse>(Error.Validation(
                "Plan.CodeExists", $"A plan with code '{code}' already exists."));
        var p = new SubscriptionPlan
        {
            Code = code, Name = r.Name.Trim(), Description = r.Description?.Trim() ?? string.Empty,
            Price = r.Price, Currency = string.IsNullOrWhiteSpace(r.Currency) ? "INR" : r.Currency.Trim().ToUpperInvariant(),
            IntervalUnit = r.IntervalUnit, IntervalCount = r.IntervalCount, IsActive = r.IsActive,
        };
        _db.SubscriptionPlans.Add(p);
        await _db.SaveChangesAsync(ct);
        return Result.Success(PlanResponse.From(p));
    }

    public async Task<Result<PlanResponse>> UpdateAsync(int id, UpdatePlanRequest r, CancellationToken ct)
    {
        var p = await _db.SubscriptionPlans.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return Result.Failure<PlanResponse>(Error.NotFound("Plan.NotFound", $"Plan {id} was not found."));

        if (r.Code is not null)
        {
            var code = r.Code.Trim();
            if (code != p.Code)
            {
                if (await _db.SubscriptionPlans.AnyAsync(x => x.Code == code && x.Id != id, ct))
                    return Result.Failure<PlanResponse>(Error.Validation(
                        "Plan.CodeExists", $"A different plan already uses code '{code}'."));
                p.Code = code;
            }
        }
        if (r.Name is not null) p.Name = r.Name.Trim();
        if (r.Description is not null) p.Description = r.Description;
        if (r.Price is not null) p.Price = r.Price.Value;
        if (r.Currency is not null) p.Currency = r.Currency.Trim().ToUpperInvariant();
        if (r.IntervalUnit is not null) p.IntervalUnit = r.IntervalUnit.Value;
        if (r.IntervalCount is not null) p.IntervalCount = r.IntervalCount.Value;
        await _db.SaveChangesAsync(ct);
        return Result.Success(PlanResponse.From(p));
    }

    public async Task<Result<PlanResponse>> SetActiveAsync(int id, bool isActive, CancellationToken ct)
    {
        var p = await _db.SubscriptionPlans.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return Result.Failure<PlanResponse>(Error.NotFound("Plan.NotFound", $"Plan {id} was not found."));
        p.IsActive = isActive;
        await _db.SaveChangesAsync(ct);
        return Result.Success(PlanResponse.From(p));
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var p = await _db.SubscriptionPlans.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return Result.Failure(Error.NotFound("Plan.NotFound", $"Plan {id} was not found."));
        var inUse = await _db.Subscriptions.AnyAsync(s => s.PlanId == id, ct);
        if (inUse)
        {
            p.IsActive = false;
            await _db.SaveChangesAsync(ct);
            return Result.Success();
        }
        _db.SubscriptionPlans.Remove(p);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<EntitlementResponse>> GetEntitlementAsync(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
            return Result.Failure<EntitlementResponse>(Error.Unauthorized(
                "Entitlement.Unauthorized", "An authenticated user is required to read entitlement."));
        var effectivePlan = await EntitlementDeriver.DeriveEffectivePlanAsync(_db, userId, DateTime.UtcNow, ct);
        return Result.Success(new EntitlementResponse(effectivePlan));
    }
}
