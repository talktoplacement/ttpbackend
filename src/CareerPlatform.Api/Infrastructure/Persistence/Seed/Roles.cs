namespace CareerPlatform.Api.Infrastructure.Persistence.Seed;

/// <summary>
/// The canonical set of role values. Roles are modelled as a string field on
/// <c>UserProfile.Role</c> (not a dedicated table), matching the legacy schema, so these
/// constants are the single source of truth for the allowed values (Req 24.5).
/// </summary>
public static class Roles
{
    public const string Student = "Student";
    public const string Admin = "Admin";

    /// <summary>All canonical role names, in a stable order.</summary>
    public static readonly IReadOnlyList<string> All = new[] { Student, Admin };
}
