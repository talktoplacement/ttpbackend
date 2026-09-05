using System.ComponentModel.DataAnnotations;

namespace CareerPlatform.Api.Configuration;

/// <summary>
/// How the session cookie is scoped across sites. Mirrors <see cref="SameSiteMode"/> but is bound
/// from configuration, so the deployment topology — not a recompile — decides the value.
/// </summary>
public enum AuthCookieSameSite
{
    /// <summary>
    /// Sent on same-site requests and top-level cross-site navigations. Correct when the browser
    /// talks to the API on the SAME site as the app (single domain, or a shared parent domain via a
    /// reverse proxy). Will NOT be sent on a cross-site fetch/XHR.
    /// </summary>
    Lax = 0,

    /// <summary>Same-site requests only. Most restrictive; breaks OAuth-style return navigations.</summary>
    Strict = 1,

    /// <summary>
    /// Sent on cross-site requests. REQUIRED when the frontend and API are on different sites — for
    /// example a Vercel-hosted app calling an API on its own domain. Browsers reject a
    /// <c>SameSite=None</c> cookie that is not also <c>Secure</c>, so this forces HTTPS.
    /// </summary>
    None = 2,
}

/// <summary>
/// Session-cookie policy, bound from the <c>AuthCookie</c> section.
///
/// This exists because the correct policy is a property of the DEPLOYMENT, not of the code. When the
/// frontend and API share a site, <c>SameSite=Lax</c> is the safer default. When they are on
/// different sites (Vercel frontend → VPS API) the browser will silently drop the cookie on every
/// XHR unless the policy is <c>None</c> + <c>Secure</c> — which presents as "login succeeds, then
/// every subsequent request is 401". Making it configurable means the same binary serves both
/// topologies, and <see cref="Validate"/> rejects the combinations browsers refuse.
/// </summary>
public sealed class AuthCookieOptions : IValidatableObject
{
    public const string Section = "AuthCookie";

    /// <summary>
    /// Cross-site policy. Defaults to <see cref="AuthCookieSameSite.None"/> because the documented
    /// production topology is split-origin (Vercel + VPS); a same-site deployment should set
    /// <c>Lax</c> explicitly.
    /// </summary>
    [EnumDataType(typeof(AuthCookieSameSite))]
    public AuthCookieSameSite SameSite { get; set; } = AuthCookieSameSite.None;

    /// <summary>
    /// Forces the <c>Secure</c> attribute regardless of whether the current request arrived over
    /// HTTPS.
    ///
    /// Needed behind a TLS-terminating proxy: the container listens on plain HTTP, so
    /// <c>Request.IsHttps</c> is only true if the proxy sets <c>X-Forwarded-Proto</c> AND forwarded
    /// headers are honoured. Deriving <c>Secure</c> from that chain means one missing proxy header
    /// silently downgrades the cookie — and for <c>SameSite=None</c> the browser then discards it
    /// outright. Leave true in any HTTPS environment; set false only for local plain-HTTP dev.
    /// </summary>
    public bool AlwaysSecure { get; set; } = true;

    /// <summary>
    /// Optional cookie <c>Domain</c>. Leave empty for a host-only cookie (the safer default).
    /// Set it only to share the cookie across subdomains of one parent domain, e.g.
    /// <c>.example.com</c>. It cannot be used to send a cookie to an unrelated site — that is what
    /// <see cref="SameSite"/> <c>None</c> is for.
    /// </summary>
    public string? Domain { get; set; }

    /// <summary>
    /// Marks the cookie <c>Partitioned</c> (CHIPS), giving it a separate jar per top-level site.
    /// Only meaningful together with <c>SameSite=None</c>; opt-in because older browsers ignore the
    /// attribute and some intermediaries mishandle it.
    /// </summary>
    public bool Partitioned { get; set; }

    /// <summary>Resolved <see cref="SameSiteMode"/> for the ASP.NET cookie API.</summary>
    public SameSiteMode ResolvedSameSite => SameSite switch
    {
        AuthCookieSameSite.Strict => SameSiteMode.Strict,
        AuthCookieSameSite.None => SameSiteMode.None,
        _ => SameSiteMode.Lax,
    };

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Browsers reject `SameSite=None` without `Secure`, so this combination would not fail
        // loudly at runtime — it would just log the user out on the next request. Fail at startup.
        if (SameSite == AuthCookieSameSite.None && !AlwaysSecure)
        {
            yield return new ValidationResult(
                $"{Section}:SameSite=None requires {Section}:AlwaysSecure=true — browsers discard a " +
                "cross-site cookie that is not marked Secure, which would break authentication.",
                [nameof(AlwaysSecure)]);
        }

        if (Partitioned && SameSite != AuthCookieSameSite.None)
        {
            yield return new ValidationResult(
                $"{Section}:Partitioned only applies to a cross-site cookie; set " +
                $"{Section}:SameSite=None or turn Partitioned off.",
                [nameof(Partitioned)]);
        }

        if (!string.IsNullOrWhiteSpace(Domain) && Uri.CheckHostName(Domain.TrimStart('.')) == UriHostNameType.Unknown)
        {
            yield return new ValidationResult(
                $"{Section}:Domain '{Domain}' is not a valid host name.", [nameof(Domain)]);
        }
    }
}
