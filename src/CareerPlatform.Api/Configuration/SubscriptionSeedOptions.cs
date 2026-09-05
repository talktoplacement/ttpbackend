using System.ComponentModel.DataAnnotations;
using CareerPlatform.Api.Features.SubscriptionPlans.Domain;

namespace CareerPlatform.Api.Configuration;

/// <summary>
/// Strongly-typed subscription-catalog seed configuration. Every tier is expressed as data
/// (Code, Name, Price, IntervalUnit, IntervalCount, Currency) so operators can add, remove, or
/// re-price plans by editing <c>.env</c>/<c>appsettings.&lt;Env&gt;.json</c> without a code change.
/// Once a plan exists in the database it is left untouched — admin CRUD (<c>UpdatePlan</c>) is
/// the source of truth after first boot, and the seeder becomes a no-op.
/// </summary>
public sealed class SubscriptionSeedOptions
{
    public const string Section = "SubscriptionSeed";

    /// <summary>Master switch. When false, the seeder is skipped entirely.</summary>
    public bool Enabled { get; set; } = true;

    public List<SubscriptionSeedPlan> Plans { get; set; } = new();
}

public sealed class SubscriptionSeedPlan
{
    [Required, StringLength(64, MinimumLength = 2)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [Range(1, 10_000_000)]
    public decimal Price { get; set; }

    [Required, StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = "INR";

    public BillingPeriodUnit IntervalUnit { get; set; } = BillingPeriodUnit.Month;

    [Range(1, 120)]
    public int IntervalCount { get; set; } = 1;

    public bool IsActive { get; set; } = true;
}
