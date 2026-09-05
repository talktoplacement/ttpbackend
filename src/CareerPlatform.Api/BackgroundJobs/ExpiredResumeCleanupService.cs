using CareerPlatform.Api.Configuration;
using CareerPlatform.Api.Features.Resumes.Domain;
using CareerPlatform.Api.Infrastructure;
using CareerPlatform.Api.Infrastructure.Concurrency;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.BackgroundJobs;

/// <summary>
/// Nightly hosted service that hard-deletes <see cref="StudentResumeUpload"/> rows whose
/// <c>ExpiresAtUtc</c> has passed, together with their object-storage blobs. The R2 bucket
/// itself is expected to have a matching 30-day lifecycle rule as a belt-and-braces backstop.
///
/// Runs every 6 hours: waits for the next tick, then processes in batches so a large backlog
/// after a downtime does not block the loop. Errors on individual rows are swallowed after
/// logging so one bad key cannot stall the entire purge.
/// </summary>
public sealed class ExpiredResumeCleanupService : BackgroundService
{
    /// <summary>
    /// Cluster-wide lock name. Every replica competes for this same name so the purge runs on
    /// exactly one instance per interval.
    /// </summary>
    internal const string LockName = "careerplatform:expired-resume-purge";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDistributedLock _distributedLock;
    private readonly ResumeRetentionOptions _options;
    private readonly ILogger<ExpiredResumeCleanupService> _logger;

    public ExpiredResumeCleanupService(
        IServiceScopeFactory scopeFactory,
        IDistributedLock distributedLock,
        IOptions<ResumeRetentionOptions> options,
        ILogger<ExpiredResumeCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _distributedLock = distributedLock;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Give the host a moment to finish starting other services before the first sweep.
        try { await Task.Delay(_options.StartupDelay, stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Single-writer guarantee across replicas: only the instance holding the advisory
                // lock purges, so blobs and rows are never deleted concurrently by two replicas.
                await using var handle =
                    await _distributedLock.TryAcquireAsync(LockName, stoppingToken);
                if (handle is not null)
                {
                    await PurgeExpiredAsync(stoppingToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "ExpiredResumeCleanupService purge failed; will retry next interval.");
            }

            try { await Task.Delay(_options.PurgeInterval, stoppingToken); }
            catch (OperationCanceledException) { return; }
        }
    }

    private async Task PurgeExpiredAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IFileStorage>();

        var now = DateTime.UtcNow;
        var totalDeleted = 0;
        var batchSize = _options.BatchSize;

        while (!ct.IsCancellationRequested)
        {
            var batch = await db.StudentResumeUploads
                .Where(x => x.ExpiresAtUtc <= now)
                .OrderBy(x => x.Id)
                .Take(batchSize)
                .ToListAsync(ct);
            if (batch.Count == 0) break;

            foreach (var row in batch)
            {
                try
                {
                    await storage.DeleteAsync(row.StorageKey, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to delete expired resume blob at key {Key}; DB row will still be removed.",
                        row.StorageKey);
                }
            }

            db.StudentResumeUploads.RemoveRange(batch);
            await db.SaveChangesAsync(ct);
            totalDeleted += batch.Count;

            if (batch.Count < batchSize) break;
        }

        if (totalDeleted > 0)
        {
            _logger.LogInformation("Purged {Count} expired resume upload(s).", totalDeleted);
        }
    }
}
