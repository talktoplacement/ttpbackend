using System.ComponentModel.DataAnnotations;
using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.AdminLedger.Domain;

/// <summary>
/// Denormalised order/invoice ledger row. Written by the payments flow (via a lifecycle hook or
/// projection) so the admin orders page can render without joining Orders + Transactions + Users
/// at read time. Distinct from <c>Order</c>/<c>Transaction</c> to keep this admin-only projection
/// decoupled from the transactional schema.
/// </summary>
public sealed class OrderInvoice : AuditableEntity<int>
{
    [Required, MaxLength(64)] public string OrderId { get; set; } = string.Empty;
    [Required, MaxLength(64)] public string CustomerUserId { get; set; } = string.Empty;
    [MaxLength(320)] public string? CustomerEmail { get; set; }
    [Required, MaxLength(500)] public string ItemDescription { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    [Required, MaxLength(8)] public string Currency { get; set; } = "INR";
    /// <summary>One of <see cref="OrderInvoiceStatus"/>.</summary>
    [Required, MaxLength(16)] public string Status { get; set; } = OrderInvoiceStatus.Pending;
    public DateTime PurchasedAtUtc { get; set; }
}
