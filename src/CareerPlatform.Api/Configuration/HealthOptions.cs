namespace CareerPlatform.Api.Configuration;

/// <summary>
/// Strongly-typed binding for the <c>Health</c> configuration section (Req 15.1).
/// Consumed by the health-check registration (task 6.1).
/// </summary>
public sealed class HealthOptions
{
    public const string Section = "Health";

    /// <summary>Overall liveness response budget in seconds (Req 16.1).</summary>
    public int LivenessTimeoutSeconds { get; init; } = 1;

    /// <summary>Overall readiness response budget in seconds (Req 16.2).</summary>
    public int ReadinessTimeoutSeconds { get; init; } = 5;

    /// <summary>Per-dependency readiness check timeout in seconds (Req 16.3).</summary>
    public int DependencyTimeoutSeconds { get; init; } = 3;
}
