using CareerPlatform.Api.Features.Coupons.Domain;

namespace CareerPlatform.Api.Features.Coupons.Dto;

public sealed record CouponResponse(
    int Id,
    string Code,
    string? Description,
    string DiscountType,
    decimal DiscountValue,
    int? MaxRedemptions,
    int RedeemedCount,
    string? StartsAtUtc,
    string? ExpiresAtUtc,
    bool IsActive)
{
    public static CouponResponse From(Coupon c)
    {
        ArgumentNullException.ThrowIfNull(c);
        return new CouponResponse(
            c.Id,
            c.Code,
            c.Description,
            c.DiscountType,
            c.DiscountValue,
            c.MaxRedemptions,
            c.RedeemedCount,
            c.StartsAtUtc?.ToString("O"),
            c.ExpiresAtUtc?.ToString("O"),
            c.IsActive);
    }
}

public sealed record CreateCouponRequest(
    string Code,
    string? Description,
    string DiscountType,
    decimal DiscountValue,
    int? MaxRedemptions,
    DateTime? StartsAtUtc,
    DateTime? ExpiresAtUtc,
    bool IsActive = true);

public sealed record UpdateCouponRequest(
    string? Description,
    string DiscountType,
    decimal DiscountValue,
    int? MaxRedemptions,
    DateTime? StartsAtUtc,
    DateTime? ExpiresAtUtc,
    bool IsActive);
