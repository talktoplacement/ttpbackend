using System.ComponentModel.DataAnnotations.Schema;
using CareerPlatform.Api.Features.Users.Domain;

namespace CareerPlatform.Api.Features.Payments.Domain;

/// <summary>
/// Payment record. Ported from the legacy entity with identical columns; only the base type
/// (<see cref="AggregateRoot{TId}"/>) and namespace change (Req 9, 24.5).
/// </summary>
public class Transaction : AggregateRoot<int>
{
    public string UserId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PlanName { get; set; } = "Free";
    public DateTime Date { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gateway order identifier used as the idempotency key for verified payment callbacks. Nullable
    /// so pre-existing (legacy) transactions keep NULL without colliding on the unique filtered index
    /// (Req 10.1). Additive column — no existing column is dropped or retyped (Req 13.1).
    /// </summary>
    public string? GatewayOrderId { get; set; }

    /// <summary>
    /// Currency snapshot at purchase time; defaults to INR (Req 8.4). Additive column (Req 13.1).
    /// </summary>
    public string Currency { get; set; } = "INR";

    [ForeignKey("UserId")]
    public UserProfile? User { get; set; }
}
