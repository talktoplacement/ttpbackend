using System.ComponentModel.DataAnnotations;

namespace CareerPlatform.Api.Configuration;

/// <summary>
/// Strongly-typed binding for the <c>Subscription</c> configuration section. Keeps the currency
/// default, the expiry-sweep cadence, and the admin catalog page cap out of source constants so they
/// are configurable and validated at startup (Req 1.5).
/// </summary>
public sealed class SubscriptionOptions
{
    public const string Section = "Subscription";

    /// <summary>Currency applied when a plan is created without an explicit currency (Req 1.5).</summary>
    [Required(AllowEmptyStrings = false)]
    public string DefaultCurrency { get; init; } = "INR";

    /// <summary>Cadence of the subscription expiry sweep.</summary>
    public TimeSpan SweepInterval { get; init; } = TimeSpan.FromMinutes(15);

    /// <summary>Maximum page size for admin plan listing.</summary>
    [Range(1, 1000)]
    public int CatalogMaxPageSize { get; init; } = 100;
}
