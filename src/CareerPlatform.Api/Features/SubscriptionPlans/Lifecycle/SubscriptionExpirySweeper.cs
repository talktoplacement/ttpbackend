using CareerPlatform.Api.Configuration;
using CareerPlatform.Api.Features.SubscriptionPlans.Domain;
using CareerPlatform.Api.Infrastructure.Concurrency;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.SubscriptionPlans.Lifecycle;

/// <summary>
/// Periodic <see cref="BackgroundService"/> that expires past-due subscriptions and reverts the
/// student's cached <c>Effective_Plan</c> to <c>"Free"</c> when no active subscription remains
/// (Req 11.2, 11.3, 11.5). Driven by a <see cref="PeriodicTimer"/> whose cadence comes from
/// <see cref="SubscriptionOptions.SweepInterval"/> (never a source literal). Each tick runs in a
/// fresh DI scope with its own <see cref="AppDbContext"/> — mirroring
/// <c>JobProcessorHostedService</c> — so scoped state never leaks between ticks.
///
/// The sweep logic is extracted into the public static <see cref="SweepAsync"/> so it can be
/// driven deterministically from tests at a fixed <c>now</c>, without the timer or the hosted-service
/// loop.
/// </summary>
public sealed class SubscriptionExpirySweeper : BackgroundService
{
    /// <summary>
    /// Cluster-wide lock name. Every replica competes for this same name so the sweep body runs on
    /// exactly one instance per tick.
    /// </summary>
    internal const string LockName = "careerplatform:subscription-expiry-sweep";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDistributedLock _distributedLock;
    private readonly SubscriptionOptions _options;
    private readonly ILogger<SubscriptionExpirySweeper> _logger;

    public SubscriptionExpirySweeper(
        IServiceScopeFactory scopeFactory,
        IDistributedLock distributedLock,
        IOptions<SubscriptionOptions> options,
        ILogger<SubscriptionExpirySweeper> logger)
    {
        _scopeFactory = scopeFactory;
        _distributedLock = distributedLock;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_options.SweepInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                // Single-writer guarantee: when the API is scaled out, only the replica that wins
                // the advisory lock performs the sweep. The others skip this tick, which prevents
                // racing UPDATEs on Subscriptions.Status and duplicated PlanName write-backs.
                await using var handle =
                    await _distributedLock.TryAcquireAsync(LockName, stoppingToken);
                if (handle is null)
                {
                    continue;
                }

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var expired = await SweepAsync(db, DateTime.UtcNow, stoppingToken);
                if (expired > 0)
                {
                    _logger.LogInformation(
                        "Subscription expiry sweep expired {Count} subscription(s).", expired);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Host shutdown — stop the loop and surface cancellation.
                throw;
            }
            catch (Exception ex)
            {
                // A failed tick must not tear down the hosted service; log and wait for the next tick.
                _logger.LogError(ex, "Subscription expiry sweep failed.");
            }
        }
    }

    /// <summary>
    /// Expires every subscription that is <see cref="SubscriptionStatus.Active"/> with an
    /// <c>EndDate</c> at or before <paramref name="nowUtc"/> (Req 11.2, 11.5). For each expired
    /// subscription, if the student has no other subscription that is active at
    /// <paramref name="nowUtc"/> (Status Active and <c>StartDate &lt;= now &lt; EndDate</c>), the
    /// student's <see cref="Features.Users.Domain.UserProfile.PlanName"/> is reverted to
    /// <c>"Free"</c> (Req 11.3). Persists all transitions in a single <c>SaveChanges</c>.
    /// </summary>
    /// <returns>The number of subscriptions transitioned to <see cref="SubscriptionStatus.Expired"/>.</returns>
    public static async Task<int> SweepAsync(AppDbContext db, DateTime nowUtc, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(db);

        var expired = await db.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.Active && s.EndDate <= nowUtc)
            .ToListAsync(ct);

        foreach (var subscription in expired)
        {
            subscription.Expire();

            // A subscription that is still active at `now` requires now < EndDate, so any other
            // past-due subscription (also being expired in this sweep) is never counted here.
            var studentStillEntitled = await db.Subscriptions.AnyAsync(other =>
                other.StudentId == subscription.StudentId &&
                other.Id != subscription.Id &&
                other.Status == SubscriptionStatus.Active &&
                other.StartDate <= nowUtc &&
                nowUtc < other.EndDate, ct);

            if (!studentStillEntitled)
            {
                var user = await db.UserProfiles
                    .FirstOrDefaultAsync(u => u.Id == subscription.StudentId, ct);
                if (user is not null)
                {
                    user.PlanName = EntitlementDeriver.FreePlan;
                }
            }
        }

        await db.SaveChangesAsync(ct);
        return expired.Count;
    }
}
