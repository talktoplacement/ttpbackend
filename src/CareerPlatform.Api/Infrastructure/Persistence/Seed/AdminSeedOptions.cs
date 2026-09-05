namespace CareerPlatform.Api.Infrastructure.Persistence.Seed;

/// <summary>
/// Bound from the <c>Seed:Admin</c> configuration section. Supplies the default administrator
/// account the <see cref="AdminSeeder"/> ensures exists. When <see cref="Email"/> is blank the
/// seeder does nothing, so no placeholder admin is ever created in an unconfigured environment.
/// Secrets (if any) come from the environment/secret store, never committed (Req 15.7).
/// </summary>
public sealed class AdminSeedOptions
{
    public const string SectionName = "Seed:Admin";

    /// <summary>Natural key used to check for an existing admin before inserting.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Display name for the seeded admin.</summary>
    public string FullName { get; set; } = "Administrator";

    /// <summary>
    /// Optional explicit primary key (e.g. the Supabase Auth UUID). When blank a new GUID string
    /// is generated. The PK is <c>ValueGeneratedNever</c>, so a value is always required at insert.
    /// </summary>
    public string? Id { get; set; }
}
