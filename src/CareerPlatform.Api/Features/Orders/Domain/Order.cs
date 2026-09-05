using System.ComponentModel.DataAnnotations;

namespace CareerPlatform.Api.Features.Orders.Domain;

/// <summary>
/// A purchase transaction for a Course or Plan. Becomes <c>Paid</c> only after server-side
/// Razorpay signature verification. Ported from the legacy entity with identical columns; only the
/// base type (<see cref="AggregateRoot{TId}"/>) and namespace change (Req 9, 24.5).
/// </summary>
public class Order : AggregateRoot<int>
{
    /// <summary>Supabase user id (string uuid).</summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>"Course" or "Plan".</summary>
    [Required]
    public string ProductType { get; set; } = string.Empty;

    public int ProductId { get; set; }

    /// <summary>Server-derived amount (INR), never taken from client input.</summary>
    public decimal Amount { get; set; }

    public string RazorpayOrderId { get; set; } = string.Empty;

    public string? RazorpayPaymentId { get; set; }

    /// <summary>"Created", "Paid", or "Failed".</summary>
    [Required]
    public string Status { get; set; } = "Created";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
