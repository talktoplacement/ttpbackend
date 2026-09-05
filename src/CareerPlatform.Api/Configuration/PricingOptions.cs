using System.ComponentModel.DataAnnotations;
using CareerPlatform.Api.Features.SubscriptionPlans.Domain;

namespace CareerPlatform.Api.Configuration;

/// <summary>
/// The operator-owned subscription price list, bound from the <c>Pricing</c> section (populated by
/// <c>application.properties</c>).
///
/// This is the single source of truth for what a plan costs. The catalog reconciler mirrors it into
/// the <c>SubscriptionPlans</c> table, and Razorpay orders are always priced from the stored plan —
/// so editing the properties file is the only action needed to change what a student is charged. No
/// code change, no redeploy.
///
/// Plans are keyed by their stable <c>Code</c>, so adding a fifth tier is a config-only change and
/// every surface (catalog API, pricing page, checkout) picks it up automatically.
/// </summary>
public sealed class PricingOptions
{
    public const string Section = "Pricing";

    /// <summary>
    /// When false the reconciler leaves the database catalog alone — useful if an operator wants to
    /// manage plans purely through admin CRUD instead of the properties file.
    /// </summary>
    public bool ReconcileFromConfig { get; set; } = true;

    /// <summary>Default currency applied to a plan that does not specify one.</summary>
    [Required, StringLength(3, MinimumLength = 3)]
    public string DefaultCurrency { get; set; } = "INR";

    /// <summary>Plan definitions keyed by stable plan code (e.g. <c>monthly-pro</c>).</summary>
    public Dictionary<string, PricingPlanEntry> Plans { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>One tier in the operator-owned price list.</summary>
public sealed class PricingPlanEntry
{
    /// <summary>Customer-facing plan name.</summary>
    [Required, StringLength(128, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Price in major currency units (whole rupees for INR). Zero is allowed so a free tier can be
    /// expressed in the same file.
    /// </summary>
    [Range(0, 10_000_000)]
    public decimal Price { get; set; }

    /// <summary>Overrides <see cref="PricingOptions.DefaultCurrency"/> when set.</summary>
    [StringLength(3, MinimumLength = 3)]
    public string? Currency { get; set; }

    /// <summary>Calendar unit of the billing period.</summary>
    public BillingPeriodUnit IntervalUnit { get; set; } = BillingPeriodUnit.Month;

    /// <summary>Number of <see cref="IntervalUnit"/> units per billing period (e.g. 3 = quarterly).</summary>
    [Range(1, 120)]
    public int IntervalCount { get; set; } = 1;

    /// <summary>Whether the tier is purchasable. Set false to retire a tier without deleting it.</summary>
    public bool Active { get; set; } = true;

    /// <summary>Sort order for display surfaces; lower comes first.</summary>
    public int DisplayOrder { get; set; }
}
