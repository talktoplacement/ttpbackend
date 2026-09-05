using CareerPlatform.Api.Features.Coupons.Domain;
using CareerPlatform.Api.Features.Coupons.Dto;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.Coupons.Service;

internal sealed class CouponService : ICouponService
{
    private readonly AppDbContext _db;

    public CouponService(AppDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<CouponResponse>>> ListAsync(CancellationToken ct)
    {
        var rows = await _db.Coupons.AsNoTracking()
            .OrderByDescending(c => c.CreatedAtUtc)
            .Take(PaginationRequest.MaxPageSize)
            .ToListAsync(ct);
        IReadOnlyList<CouponResponse> items = rows.Select(CouponResponse.From).ToList();
        return Result.Success(items);
    }

    public async Task<Result<CouponResponse>> GetAsync(int id, CancellationToken ct)
    {
        var c = await _db.Coupons.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null)
        {
            return Result.Failure<CouponResponse>(Error.NotFound(
                "Coupon.NotFound", $"Coupon {id} was not found."));
        }
        return Result.Success(CouponResponse.From(c));
    }

    public async Task<Result<CouponResponse>> CreateAsync(CreateCouponRequest r, CancellationToken ct)
    {
        var code = r.Code.Trim().ToUpperInvariant();
        if (await _db.Coupons.AnyAsync(c => c.Code == code, ct))
        {
            return Result.Failure<CouponResponse>(Error.Validation(
                "Coupon.CodeExists", $"A coupon with code '{code}' already exists."));
        }
        var coupon = new Coupon
        {
            Code = code,
            Description = r.Description?.Trim(),
            DiscountType = r.DiscountType.Trim().ToLowerInvariant(),
            DiscountValue = r.DiscountValue,
            MaxRedemptions = r.MaxRedemptions,
            StartsAtUtc = r.StartsAtUtc,
            ExpiresAtUtc = r.ExpiresAtUtc,
            IsActive = r.IsActive,
        };
        _db.Coupons.Add(coupon);
        await _db.SaveChangesAsync(ct);
        return Result.Success(CouponResponse.From(coupon));
    }

    public async Task<Result<CouponResponse>> UpdateAsync(int id, UpdateCouponRequest r, CancellationToken ct)
    {
        var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (coupon is null)
        {
            return Result.Failure<CouponResponse>(Error.NotFound(
                "Coupon.NotFound", $"Coupon {id} was not found."));
        }
        coupon.Description = r.Description?.Trim();
        coupon.DiscountType = r.DiscountType.Trim().ToLowerInvariant();
        coupon.DiscountValue = r.DiscountValue;
        coupon.MaxRedemptions = r.MaxRedemptions;
        coupon.StartsAtUtc = r.StartsAtUtc;
        coupon.ExpiresAtUtc = r.ExpiresAtUtc;
        coupon.IsActive = r.IsActive;
        await _db.SaveChangesAsync(ct);
        return Result.Success(CouponResponse.From(coupon));
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (coupon is null)
        {
            return Result.Failure(Error.NotFound(
                "Coupon.NotFound", $"Coupon {id} was not found."));
        }
        // Soft-retire coupons that have been redeemed so historical order data stays consistent.
        if (coupon.RedeemedCount > 0)
        {
            coupon.IsActive = false;
        }
        else
        {
            _db.Coupons.Remove(coupon);
        }
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
