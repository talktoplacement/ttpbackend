using System.ComponentModel.DataAnnotations;
using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.Products.Domain;

/// <summary>
/// One-time SKUs and add-ons (mentorship sessions, career pack, mock interview credit). Distinct
/// from <c>SubscriptionPlans</c> which are recurring. Used for stand-alone Razorpay checkouts
/// where a plan-subscription model doesn't fit.
/// </summary>
public sealed class Product : AuditableEntity<int>
{
    [Required, MaxLength(64)] public string Code { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
    [MaxLength(2000)] public string? Description { get; set; }

    /// <summary><c>one-time</c> / <c>add-on</c> / <c>consultation</c>.</summary>
    [Required, MaxLength(32)] public string ProductType { get; set; } = "one-time";

    public decimal Price { get; set; }

    [Required, MaxLength(8)] public string Currency { get; set; } = "INR";

    public bool IsActive { get; set; } = true;
}
