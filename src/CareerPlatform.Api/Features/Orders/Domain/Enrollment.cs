using System.ComponentModel.DataAnnotations;

namespace CareerPlatform.Api.Features.Orders.Domain;

/// <summary>
/// A confirmed grant of access for a user to a Course or Plan, created from a paid + verified
/// <see cref="Order"/>. Ported from the legacy entity with identical columns; only the base type
/// (<see cref="AggregateRoot{TId}"/>) and namespace change (Req 9, 24.5).
/// </summary>
public class Enrollment : AggregateRoot<int>
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>"Course" or "Plan".</summary>
    [Required]
    public string ProductType { get; set; } = string.Empty;

    public int ProductId { get; set; }

    public int OrderId { get; set; }

    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
}
