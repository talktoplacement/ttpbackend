using CareerPlatform.Api.Configuration;
using CareerPlatform.Api.Infrastructure.Concurrency;

namespace CareerPlatform.Api.Features.SubscriptionPlans.Lifecycle;

/// <summary>
/// Keeps the stored subscription catalog in step with <c>application.properties</c>.
///
/// Runs the reconciler once at startup and again whenever the pricing configuration changes on disk
/// (the properties file is registered with <c>reloadOnChange</c>). That is what delivers the
/// operational requirement: edit the price in the properties file and the change takes effect on the
/// running instance — Razorpay then charges the new amount because orders are always priced from the
/// stored plan.
///
/// Reconciliation is guarded by the same distributed lock pattern as the other periodic jobs, so on a
/// horizontally-scaled deployment only one replica writes.
/// </summary>
public sealed class PricingReconciliationService : BackgroundService
{
    /// <summary>Cluster-wide lock name; every replica competes for this one key.</summary>
    internal const string LockName = "careerplatform:pricing-reconcile";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDistributedLock _distributedLock;
    private readonly IOptionsMonitor<PricingOptions> _pricing;
    private readonly ILogger<PricingReconciliationService> _logger;

    public PricingReconciliationService(
        IServiceScopeFactory scopeFactory,
        IDistributedLock distributedLock,
        IOptionsMonitor<PricingOptions> pricing,
        ILogger<PricingReconciliationService> logger)
    {
        _scopeFactory = scopeFactory;
        _distributedLock = distributedLock;
        _pricing = pricing;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Initial pass so a fresh database (or a price edited while the process was down) is applied.
        await ReconcileSafelyAsync(stoppingToken);

        // React to subsequent edits of the properties file.
        using var subscription = _pricing.OnChange(updated =>
        {
            _logger.LogInformation(
                "Pricing configuration changed ({Count} plan(s) in file); reconciling catalog.",
                updated.Plans.Count);

            // OnChange callbacks are synchronous; hand off so we never block the file watcher, and
            // never let an exception escape into it.
            _ = Task.Run(() => ReconcileSafelyAsync(stoppingToken), CancellationToken.None);
        });

        // Idle until shutdown; all further work is change-driven.
        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
    }

    private async Task ReconcileSafelyAsync(CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return;

        try
        {
            await using var handle = await _distributedLock.TryAcquireAsync(LockName, ct);
            if (handle is null)
            {
                // Another replica is reconciling the same config; its result is authoritative.
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var reconciler = scope.ServiceProvider.GetRequiredService<SubscriptionCatalogReconciler>();
            var changes = await reconciler.ReconcileAsync(_pricing.CurrentValue, ct);

            if (changes > 0)
            {
                _logger.LogInformation(
                    "Pricing reconciliation applied {Count} catalog change(s) from configuration.", changes);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Shutting down.
        }
        catch (Exception ex)
        {
            // A failed reconcile must never take the host down; the catalog simply keeps its last
            // known-good state until the next attempt.
            _logger.LogError(ex, "Pricing reconciliation failed; the stored catalog is unchanged.");
        }
    }
}
