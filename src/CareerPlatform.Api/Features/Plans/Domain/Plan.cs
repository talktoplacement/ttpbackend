using System.ComponentModel.DataAnnotations;

namespace CareerPlatform.Api.Features.Plans.Domain;

/// <summary>
/// A subscription/tier granting access to a set of products. Ported from the legacy entity with
/// identical columns; only the base type (<see cref="AggregateRoot{TId}"/>) and namespace change
/// (Req 9, 24.5).
/// </summary>
public class Plan : AggregateRoot<int>
{
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>Price in INR (whole rupees). Server-controlled.</summary>
    public decimal Price { get; set; } = 0;

    /// <summary>Billing interval, e.g. "Monthly", "Yearly", "OneTime".</summary>
    [Required]
    public string Interval { get; set; } = "OneTime";

    public bool IsPublished { get; set; } = true;

    /// <summary>Entitlement keys granted by this plan (Npgsql maps to text[]).</summary>
    public List<string> Entitlements { get; set; } = new();
}
