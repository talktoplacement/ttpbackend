using CareerPlatform.Api.Features.Users.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CareerPlatform.Api.Infrastructure.Persistence.Seed;

/// <summary>
/// Ensures a default administrator <see cref="UserProfile"/> exists. Existence is checked by the
/// natural key (<c>Email</c>) before inserting, so repeated runs never create duplicate admins
/// and leave the row identical to a single run (Req 17.8). When no admin email is configured the
/// seeder is a no-op, so an unconfigured environment gets no placeholder account.
/// </summary>
public sealed class AdminSeeder : ISeeder
{
    private readonly AppDbContext _db;
    private readonly AdminSeedOptions _options;
    private readonly ILogger<AdminSeeder> _logger;

    public AdminSeeder(
        AppDbContext db, IOptions<AdminSeedOptions> options, ILogger<AdminSeeder> logger)
    {
        _db = db;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>Runs after <see cref="RoleSeeder"/> so the admin role is well-defined.</summary>
    public int Order => 10;

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Email))
        {
            _logger.LogInformation("AdminSeeder: no admin email configured; skipping.");
            return;
        }

        // Natural-key existence check: repeated runs are a no-op once the admin exists (Req 17.8).
        var exists = await _db.UserProfiles
            .AnyAsync(u => u.Email == _options.Email, cancellationToken);
        if (exists)
        {
            _logger.LogInformation(
                "AdminSeeder: admin {Email} already present; nothing to do.", _options.Email);
            return;
        }

        var admin = new UserProfile
        {
            Email = _options.Email,
            FullName = _options.FullName,
            Role = Roles.Admin,
            PlanName = "Free",
            CreatedAt = DateTime.UtcNow,
        };

        var entry = _db.UserProfiles.Add(admin);

        // UserProfile.Id has a protected setter and is ValueGeneratedNever, so assign the PK
        // through the EF entry. Use the configured id (e.g. a Supabase UUID) or a new GUID.
        var id = string.IsNullOrWhiteSpace(_options.Id) ? Guid.NewGuid().ToString() : _options.Id!;
        entry.Property(u => u.Id).CurrentValue = id;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("AdminSeeder: created admin {Email} (id {Id}).", _options.Email, id);
    }
}
