using CareerPlatform.Api.Configuration;
using CareerPlatform.Api.Features.SubscriptionPlans.Domain;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.SubscriptionPlans.Lifecycle;

/// <summary>
/// Makes the <c>SubscriptionPlans</c> table a materialised view of the operator's price list in
/// <c>application.properties</c>.
///
/// Why this exists: pricing must be changeable by editing one config file, with no code change and
/// no redeploy — but Razorpay orders must still be priced from a stored plan row so that an order id
/// can be bound to a plan and amount server-side. Reconciling config into the table satisfies both:
/// config stays authoritative, while the rest of the system keeps a stable <c>PlanId</c> to
/// reference.
///
/// Behaviour:
/// <list type="bullet">
///   <item>Upserts by the natural key <c>Code</c> — a price edit updates the existing row, so
///   historical <c>PlanId</c> references (subscriptions, orders) stay valid.</item>
///   <item>Deactivates rows whose code disappears from config rather than deleting them, because
///   past subscriptions and orders still point at them.</item>
///   <item>Is idempotent: with unchanged config it performs no writes.</item>
/// </list>
/// </summary>
public sealed class SubscriptionCatalogReconciler
{
    private readonly AppDbContext _db;
    private readonly ILogger<SubscriptionCatalogReconciler> _logger;

    public SubscriptionCatalogReconciler(
        AppDbContext db, ILogger<SubscriptionCatalogReconciler> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Applies <paramref name="pricing"/> to the stored catalog. Returns the number of rows changed
    /// (0 when the catalog already matches, which is the steady state).
    /// </summary>
    public async Task<int> ReconcileAsync(PricingOptions pricing, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(pricing);

        if (!pricing.ReconcileFromConfig)
        {
            _logger.LogInformation(
                "Pricing reconciliation disabled (Pricing:ReconcileFromConfig=false); leaving the catalog untouched.");
            return 0;
        }
        if (pricing.Plans.Count == 0)
        {
            // Never treat an empty price list as "retire everything" — an unreadable or
            // partially-written properties file would otherwise take the whole catalog offline.
            _logger.LogWarning(
                "Pricing configuration contains no plans; skipping reconciliation to avoid deactivating the live catalog.");
            return 0;
        }

        var existing = await _db.SubscriptionPlans.ToListAsync(ct);
        var byCode = existing.ToDictionary(p => p.Code, StringComparer.OrdinalIgnoreCase);
        var changes = 0;

        foreach (var (code, entry) in pricing.Plans)
        {
            var normalizedCode = code.Trim();
            if (normalizedCode.Length == 0) continue;

            var currency = string.IsNullOrWhiteSpace(entry.Currency)
                ? pricing.DefaultCurrency
                : entry.Currency!;

            if (byCode.TryGetValue(normalizedCode, out var plan))
            {
                if (ApplyTo(plan, entry, currency))
                {
                    changes++;
                    _logger.LogInformation(
                        "Pricing reconcile: updated plan {Code} to {Price} {Currency} per {Count} {Unit}(s), active={Active}.",
                        normalizedCode, entry.Price, currency, entry.IntervalCount, entry.IntervalUnit, entry.Active);
                }
                continue;
            }

            var created = new SubscriptionPlan { Code = normalizedCode };
            ApplyTo(created, entry, currency);
            _db.SubscriptionPlans.Add(created);
            changes++;
            _logger.LogInformation(
                "Pricing reconcile: created plan {Code} at {Price} {Currency}.",
                normalizedCode, entry.Price, currency);
        }

        // Codes no longer present in config are retired, not deleted: existing subscriptions and
        // order rows reference them by id.
        foreach (var orphan in existing.Where(p => p.IsActive && !pricing.Plans.ContainsKey(p.Code)))
        {
            orphan.IsActive = false;
            changes++;
            _logger.LogInformation(
                "Pricing reconcile: deactivated plan {Code} (absent from configuration).", orphan.Code);
        }

        if (changes > 0)
        {
            await _db.SaveChangesAsync(ct);
        }
        return changes;
    }

    /// <summary>
    /// Copies config values onto the entity. Returns whether anything actually changed, so a
    /// no-op reconcile issues no UPDATE.
    /// </summary>
    private static bool ApplyTo(SubscriptionPlan plan, PricingPlanEntry entry, string currency)
    {
        var description = entry.Description ?? string.Empty;
        var changed =
            plan.Name != entry.Name ||
            plan.Description != description ||
            plan.Price != entry.Price ||
            plan.Currency != currency ||
            plan.IntervalUnit != entry.IntervalUnit ||
            plan.IntervalCount != entry.IntervalCount ||
            plan.IsActive != entry.Active;

        if (!changed) return false;

        plan.Name = entry.Name;
        plan.Description = description;
        plan.Price = entry.Price;
        plan.Currency = currency;
        plan.IntervalUnit = entry.IntervalUnit;
        plan.IntervalCount = entry.IntervalCount;
        plan.IsActive = entry.Active;
        return true;
    }
}
