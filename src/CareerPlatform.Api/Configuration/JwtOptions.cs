using System.ComponentModel.DataAnnotations;

namespace CareerPlatform.Api.Configuration;

/// <summary>
/// Strongly-typed binding for the <c>Jwt</c> configuration section (Req 15.1).
/// The signing <see cref="Secret"/> is required; a missing value fails startup
/// validation (Req 15.2, 15.3).
/// </summary>
public sealed class JwtOptions
{
    public const string Section = "Jwt";

    /// <summary>
    /// Expected token issuer (<c>iss</c>). Required — a missing/empty issuer combined with
    /// <c>ValidateIssuer = true</c> would otherwise reject every request at runtime, so we fail
    /// fast at startup instead (Req 15.2, 15.3).
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string Issuer { get; init; } = string.Empty;

    /// <summary>
    /// Expected token audience (<c>aud</c>). Required for the same fail-fast reason as
    /// <see cref="Issuer"/>.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string Audience { get; init; } = string.Empty;

    /// <summary>
    /// Symmetric signing key. Required and must be at least 32 characters so the derived HMAC key
    /// meets the 256-bit minimum for HS256; a weak key compromises every token. Startup fails fast
    /// when absent or too short (Req 15.2, 15.3).
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    [MinLength(32, ErrorMessage = "Jwt:Secret must be at least 32 characters (256 bits) for HS256.")]
    public string Secret { get; init; } = string.Empty;

    /// <summary>Claim carrying the caller's role(s). Defaults to <c>role</c>.</summary>
    public string RoleClaim { get; init; } = "role";
}
