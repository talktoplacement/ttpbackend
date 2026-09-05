using Microsoft.Extensions.DependencyInjection;

namespace CareerPlatform.Api.Configuration;

/// <summary>
/// Registers every strongly-typed options concern with fail-fast validation and
/// provides the startup environment check (Req 15).
/// </summary>
public static class OptionsRegistration
{
    /// <summary>The only environment names the application recognizes (Req 15.6).</summary>
    private static readonly string[] RecognizedEnvironments =
        [Environments.Development, Environments.Staging, Environments.Production];

    /// <summary>
    /// Binds each configuration concern to its options type. Secret-bearing options
    /// (<see cref="JwtOptions"/>, <see cref="RazorpayOptions"/>, <see cref="BrevoOptions"/>)
    /// use DataAnnotations validation evaluated at startup so misconfiguration halts the
    /// app before it accepts requests (Req 15.2, 15.3). Non-secret options are bound and,
    /// where applicable, clamped to safe ranges.
    /// </summary>
    public static IServiceCollection AddHardenedOptions(
        this IServiceCollection services, IConfiguration configuration)
    {
        // --- Options validated at startup (fail-fast on missing/invalid required values) ---
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<RazorpayOptions>()
            .Bind(configuration.GetSection(RazorpayOptions.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<BrevoOptions>()
            .Bind(configuration.GetSection(BrevoOptions.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<OtpOptions>()
            .Bind(configuration.GetSection(OtpOptions.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // --- Rate limiting: bind then clamp to accepted ranges (defaults 100 / 60) ---
        services.AddOptions<RateLimitOptions>()
            .Bind(configuration.GetSection(RateLimitOptions.Section))
            .PostConfigure(o =>
            {
                o.PermitLimit = Math.Clamp(o.PermitLimit, 1, 10_000);
                o.WindowSeconds = Math.Clamp(o.WindowSeconds, 1, 3_600);
            });

        // --- Remaining non-secret options: straight binds ---
        services.AddOptions<CorsOptions>()
            .Bind(configuration.GetSection(CorsOptions.Section));

        // Session-cookie policy. Validated at startup because the invalid combinations here do not
        // fail loudly at runtime — a SameSite=None cookie without Secure is silently discarded by the
        // browser, which presents as "login works, then everything is 401". ValidateDataAnnotations
        // also runs the IValidatableObject rules on AuthCookieOptions.
        services.AddOptions<AuthCookieOptions>()
            .Bind(configuration.GetSection(AuthCookieOptions.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<ObservabilityOptions>()
            .Bind(configuration.GetSection(ObservabilityOptions.Section));

        services.AddOptions<StorageOptions>()
            .Bind(configuration.GetSection(StorageOptions.Section));

        services.AddOptions<HealthOptions>()
            .Bind(configuration.GetSection(HealthOptions.Section));

        services.AddOptions<CacheOptions>()
            .Bind(configuration.GetSection(CacheOptions.Section));

        // Expired-resume purge cadence/batching — validated so a misconfigured batch size halts
        // startup rather than producing a runaway or no-op sweep.
        services.AddOptions<ResumeRetentionOptions>()
            .Bind(configuration.GetSection(ResumeRetentionOptions.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Operator-owned price list from application.properties. Bound (not snapshot) so
        // IOptionsMonitor delivers file edits to the pricing reconciler at runtime. Validated so a
        // typo — a negative price, a missing name — is rejected instead of silently mispricing a
        // plan. ValidateOnStart is deliberately NOT used here: an invalid *edit* on a running
        // instance must not crash the host, and the reconciler already fails soft.
        services.AddOptions<PricingOptions>()
            .Bind(configuration.GetSection(PricingOptions.Section))
            .ValidateDataAnnotations();

        // Code-execution sandbox for coding assessments. Validated at startup so a bad provider name
        // or an out-of-range timeout is caught before any student submits an answer.
        services.AddOptions<CodeExecutionOptions>()
            .Bind(configuration.GetSection(CodeExecutionOptions.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Subscription pricing options: bound and validated at startup so a misconfigured
        // currency/page-size halts the app before it serves requests (Req 1.5).
        services.AddOptions<SubscriptionOptions>()
            .Bind(configuration.GetSection(SubscriptionOptions.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();

// Subscription-catalog seed: data-driven plan definitions (Code/Name/Price/Interval)
        // read from configuration so operators can add or re-price tiers without a code change.
        services.AddOptions<SubscriptionSeedOptions>()
            .Bind(configuration.GetSection(SubscriptionSeedOptions.Section));

        return services;
    }

    /// <summary>
    /// Verifies the active hosting environment is one of Development, Staging, or
    /// Production, throwing an identifying error otherwise so startup halts (Req 15.6).
    /// Call from Program.cs before <c>app.Run()</c>.
    /// </summary>
    public static void ValidateEnvironment(IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);

        var name = environment.EnvironmentName;
        if (!RecognizedEnvironments.Contains(name, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                $"Unrecognized hosting environment '{name}'. " +
                $"The environment must be one of: {string.Join(", ", RecognizedEnvironments)}.");
        }
    }
}
