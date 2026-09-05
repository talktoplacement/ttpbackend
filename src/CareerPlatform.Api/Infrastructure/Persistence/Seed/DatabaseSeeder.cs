using Microsoft.Extensions.Logging;

namespace CareerPlatform.Api.Infrastructure.Persistence.Seed;

/// <summary>
/// Orchestrates the registered <see cref="ISeeder"/>s, running them in ascending
/// <see cref="ISeeder.Order"/>. Each seeder is individually idempotent (existence-checked by
/// natural key), so the whole run is idempotent: calling <see cref="SeedAsync"/> repeatedly
/// produces no duplicate rows (Req 17.8). This is NOT invoked automatically at startup — callers
/// run it explicitly (e.g. a deploy/CLI step) against a resolved DI scope.
/// </summary>
public sealed class DatabaseSeeder
{
    private readonly IEnumerable<ISeeder> _seeders;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(IEnumerable<ISeeder> seeders, ILogger<DatabaseSeeder> logger)
    {
        _seeders = seeders;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        foreach (var seeder in _seeders.OrderBy(s => s.Order))
        {
            _logger.LogInformation("Running seeder {Seeder}.", seeder.GetType().Name);
            await seeder.SeedAsync(cancellationToken);
        }

        _logger.LogInformation("Database seeding complete.");
    }
}
