using System.ComponentModel.DataAnnotations;
using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.Coupons.Domain;

/// <summary>
/// A discount coupon redeemable at checkout. Backend-owned; the redemption flow is the
/// authoritative check for expiry / active / redemption-cap — the client only reads state.
/// </summary>
public sealed class Coupon : AuditableEntity<int>
{
    [Required, MaxLength(64)] public string Code { get; set; } = string.Empty;

    [MaxLength(500)] public string? Description { get; set; }

    /// <summary><c>percent</c> (0-100) or <c>flat</c> (INR).</summary>
    [Required, MaxLength(16)] public string DiscountType { get; set; } = "percent";

    public decimal DiscountValue { get; set; }

    /// <summary>Total number of redemptions allowed; <c>null</c> = unlimited.</summary>
    public int? MaxRedemptions { get; set; }

    /// <summary>How many times this coupon has been used so far.</summary>
    public int RedeemedCount { get; set; }

    public DateTime? StartsAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }

    public bool IsActive { get; set; } = true;
}
