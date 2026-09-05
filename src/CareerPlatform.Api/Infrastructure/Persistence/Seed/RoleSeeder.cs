using Microsoft.Extensions.Logging;

namespace CareerPlatform.Api.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds the canonical role set. In the current (legacy-preserved) schema, roles are string
/// values on <c>UserProfile.Role</c> rather than rows in a dedicated Roles table (Req 24.5,
/// non-destructive). There is therefore no table to populate: this seeder is intentionally a
/// database no-op and is idempotent by construction. It exists as the seam where role
/// bootstrapping lives, so that if a Roles table is ever introduced its seeding is added here
/// without touching the orchestrator (Req 17.8).
/// </summary>
public sealed class RoleSeeder : ISeeder
{
    private readonly ILogger<RoleSeeder> _logger;

    public RoleSeeder(ILogger<RoleSeeder> logger) => _logger = logger;

    /// <summary>Runs before <see cref="AdminSeeder"/> so the admin's role is well-defined.</summary>
    public int Order => 0;

    public Task SeedAsync(CancellationToken cancellationToken)
    {
        // Roles are string-valued on UserProfile; no rows to insert. Repeated runs are no-ops.
        _logger.LogInformation(
            "RoleSeeder: canonical roles are {Roles} (string-valued on UserProfile; no table to seed).",
            string.Join(", ", Roles.All));
        return Task.CompletedTask;
    }
}
