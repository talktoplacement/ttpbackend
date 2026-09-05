using System.ComponentModel.DataAnnotations;

namespace CareerPlatform.Api.Configuration;

/// <summary>
/// Configuration for the registration email-OTP flow. Every knob is data — expiry, code length,
/// retry limits, and the HMAC key that hashes stored codes — so operators can tune the flow
/// without touching code.
/// </summary>
public sealed class OtpOptions
{
    public const string Section = "Otp";

    /// <summary>Number of digits in the generated code (default 6).</summary>
    [Range(4, 10)]
    public int CodeLength { get; set; } = 6;

    /// <summary>How long a freshly-issued code remains valid, in seconds (default 600 = 10min).</summary>
    [Range(60, 3600)]
    public int ExpirySeconds { get; set; } = 600;

    /// <summary>Maximum wrong verification attempts before the code is invalidated (default 5).</summary>
    [Range(1, 20)]
    public int MaxAttempts { get; set; } = 5;

    /// <summary>Minimum seconds between resend requests for the same email (default 60).</summary>
    [Range(10, 3600)]
    public int ResendCooldownSeconds { get; set; } = 60;

    /// <summary>
    /// HMAC-SHA256 key used to hash OTP codes at rest so a database dump does not expose codes
    /// (defence-in-depth over BCrypt-hashed passwords). Minimum 32 chars, kept in server-only
    /// configuration — never in the browser.
    /// </summary>
    [Required, MinLength(32)]
    public string HashKey { get; set; } = string.Empty;
}
