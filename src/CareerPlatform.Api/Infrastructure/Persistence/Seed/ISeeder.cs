namespace CareerPlatform.Api.Infrastructure.Persistence.Seed;

/// <summary>
/// A single idempotent seed unit. Every implementation checks existence by natural key before
/// inserting, so repeated runs produce no duplicate rows and leave seeded data identical to a
/// single run (Req 17.8). The <see cref="DatabaseSeeder"/> orchestrator runs registered seeders
/// in ascending <see cref="Order"/>.
/// </summary>
public interface ISeeder
{
    /// <summary>Relative execution order; lower runs first (e.g. roles before the admin user).</summary>
    int Order { get; }

    /// <summary>
    /// Applies this seeder's data idempotently. Implementations MUST be safe to call repeatedly.
    /// </summary>
    Task SeedAsync(CancellationToken cancellationToken);
}
