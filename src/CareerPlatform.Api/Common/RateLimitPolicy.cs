namespace CareerPlatform.Api.Common;

/// <summary>
/// Names and pure helpers for the API rate-limiting policies. Kept free of framework/DI
/// dependencies so the arithmetic can be unit/property-tested in isolation.
/// </summary>
/// <remarks>Requirements 21.1, 21.2, 21.3, 21.5.</remarks>
public static class RateLimitPolicy
{
    /// <summary>
    /// Name of the fixed-window policy applied to sensitive/administrative endpoints
    /// (partitioned by authenticated subject → client IP → <c>"anonymous"</c>).
    /// </summary>
    public const string Sensitive = "sensitive";

    /// <summary>
    /// Name of the stricter fixed-window policy for endpoints that run untrusted code in a sandbox.
    ///
    /// Separate from <see cref="Sensitive"/> because the cost profile is different in kind: each call
    /// spawns a sandbox process, so the limit has to bound compute, not just request volume. One
    /// authenticated user hammering the runner could otherwise starve the box that also serves the
    /// API. Partitioned by authenticated subject, so the budget is per-user.
    /// </summary>
    public const string CodeExecution = "code-execution";

    /// <summary>
    /// Computes the <c>Retry-After</c> value (whole seconds) from the seconds remaining until the
    /// client's window resets. Always at least 1 second so a rejected caller never receives a
    /// non-positive hint (Req 21.3).
    /// </summary>
    public static int RetryAfterSeconds(double remainingSeconds)
        => Math.Max(1, (int)Math.Ceiling(remainingSeconds));

    /// <summary>
    /// Computes the <c>Retry-After</c> value from a window length and the elapsed time within it:
    /// <c>max(1, ceil(window - elapsed))</c>. Returns 1 when elapsed meets or exceeds the window
    /// (Req 21.3).
    /// </summary>
    public static int RetryAfterSeconds(int windowSeconds, double elapsedSeconds)
        => RetryAfterSeconds(windowSeconds - elapsedSeconds);
}
