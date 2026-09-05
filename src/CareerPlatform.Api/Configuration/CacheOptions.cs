namespace CareerPlatform.Api.Configuration;

/// <summary>
/// Strongly-typed binding for the <c>Cache</c> configuration section (Req 15.1).
/// Consumed by the Redis cache adapter (task 7.2).
/// </summary>
public sealed class CacheOptions
{
    public const string Section = "Cache";

    /// <summary>Logical Redis instance name / key prefix.</summary>
    public string? InstanceName { get; init; }

    /// <summary>Default entry time-to-live in seconds.</summary>
    public int DefaultTtlSeconds { get; init; } = 300;
}
