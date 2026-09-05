using System.ComponentModel.DataAnnotations;

namespace CareerPlatform.Api.Configuration;

/// <summary>
/// Strongly-typed binding for the <c>ResumeRetention</c> configuration section. Keeps the
/// expired-resume purge cadence, batch size, and startup delay out of source constants so operators
/// can tune them per environment without a code change.
/// </summary>
public sealed class ResumeRetentionOptions
{
    public const string Section = "ResumeRetention";

    /// <summary>How often the expired-resume purge runs.</summary>
    public TimeSpan PurgeInterval { get; init; } = TimeSpan.FromHours(6);

    /// <summary>
    /// Rows deleted per batch. Batching keeps a large backlog from holding a single long
    /// transaction after downtime.
    /// </summary>
    [Range(1, 10_000)]
    public int BatchSize { get; init; } = 200;

    /// <summary>
    /// Grace period after host start before the first purge, so the sweep does not compete with
    /// application warm-up.
    /// </summary>
    public TimeSpan StartupDelay { get; init; } = TimeSpan.FromMinutes(1);
}
