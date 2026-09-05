namespace CareerPlatform.Api.Configuration;

/// <summary>
/// Strongly-typed binding for the <c>RateLimiting</c> configuration section (Req 15.1).
/// Values are clamped to their accepted ranges during registration
/// (<see cref="OptionsRegistration.AddHardenedOptions"/>).
/// </summary>
public sealed class RateLimitOptions
{
    public const string Section = "RateLimiting";

    /// <summary>Requests permitted per window. Clamped to [1, 10000]; default 100.</summary>
    public int PermitLimit { get; set; } = 100;

    /// <summary>Fixed-window length in seconds. Clamped to [1, 3600]; default 60.</summary>
    public int WindowSeconds { get; set; } = 60;
}
