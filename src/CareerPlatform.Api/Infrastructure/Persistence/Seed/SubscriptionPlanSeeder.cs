using CareerPlatform.Api.Configuration;
using CareerPlatform.Api.Features.SubscriptionPlans.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CareerPlatform.Api.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds the launch subscription catalog from <see cref="SubscriptionSeedOptions"/> — every plan
/// (Code, Name, Price, IntervalUnit, IntervalCount) is data, not code, so operators re-tier the
/// catalog by editing <c>.env</c>/<c>appsettings.&lt;Env&gt;.json</c> without a code change
/// (Req 13.3, 13.4, 13.5). Existence is checked by <c>Code</c> before inserting, so repeated
/// runs never create duplicates (Req 17.8) and admin edits made through <c>UpdatePlan</c>
/// are preserved.
/// </summary>
public sealed class SubscriptionPlanSeeder : ISeeder
{
    private readonly AppDbContext _db;
    private readonly IOptions<SubscriptionSeedOptions> _options;
    private readonly ILogger<SubscriptionPlanSeeder> _logger;

    public SubscriptionPlanSeeder(
        AppDbContext db,
        IOptions<SubscriptionSeedOptions> options,
        ILogger<SubscriptionPlanSeeder> logger)
    {
        _db = db;
        _options = options;
        _logger = logger;
    }

    /// <summary>Runs after the role/admin seeders.</summary>
    public int Order => 50;

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var config = _options.Value;
        if (!config.Enabled)
        {
            _logger.LogInformation("SubscriptionPlanSeeder disabled by configuration; skipping.");
            return;
        }

        if (config.Plans.Count == 0)
        {
            _logger.LogWarning(
                "SubscriptionPlanSeeder enabled but no plans configured under {Section}:Plans. Nothing to seed.",
                SubscriptionSeedOptions.Section);
            return;
        }

        foreach (var plan in config.Plans)
        {
            await EnsurePlanAsync(plan, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsurePlanAsync(SubscriptionSeedPlan plan, CancellationToken cancellationToken)
    {
        // Natural-key existence check: repeated runs are a no-op once the plan exists (Req 13.5).
        // We deliberately do NOT UPSERT — after first boot, admin edits via UpdatePlan own the row.
        var exists = await _db.SubscriptionPlans.AnyAsync(p => p.Code == plan.Code, cancellationToken);
        if (exists)
        {
            _logger.LogInformation(
                "SubscriptionPlanSeeder: plan {Code} already present; leaving admin-owned data intact.",
                plan.Code);
            return;
        }

        _db.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Code = plan.Code,
            Name = plan.Name,
            Description = plan.Description ?? string.Empty,
            Price = plan.Price,
            Currency = string.IsNullOrWhiteSpace(plan.Currency) ? "INR" : plan.Currency,
            IntervalUnit = plan.IntervalUnit,
            IntervalCount = plan.IntervalCount,
            IsActive = plan.IsActive,
        });

        _logger.LogInformation(
            "SubscriptionPlanSeeder: seeding plan {Code} ({Name}) at {Price} {Currency} per {Count} {Unit}(s).",
            plan.Code, plan.Name, plan.Price, plan.Currency, plan.IntervalCount, plan.IntervalUnit);
    }
}
