using CareerPlatform.Api.Configuration;

namespace CareerPlatform.Api.Infrastructure.Security;

/// <summary>
/// The name of the browser session cookie that carries the signed JWT.
///
/// Kept as a static constant rather than folded into <see cref="IAuthSessionCookie"/> because the
/// read side — the JWT bearer handler's <c>OnMessageReceived</c> — runs before any feature service is
/// resolved and only needs the name. Writing the cookie needs the deployment's policy, so that lives
/// behind <see cref="IAuthSessionCookie"/>.
/// </summary>
public static class AuthCookie
{
    /// <summary>Cookie name. Kept in sync with the bearer handler and the frontend.</summary>
    public const string Name = "access_token";
}

/// <summary>
/// Issues and revokes the session cookie.
///
/// The cookie is <c>HttpOnly</c> so it is never readable from JavaScript (an XSS payload cannot
/// exfiltrate the token) and is consumed by the JWT bearer handler, which reads the token from it
/// when no <c>Authorization</c> header is present. Because the value is a standard signed JWT,
/// server-side middleware (including the Next.js edge proxy) can verify it authoritatively rather
/// than trusting a client-writable role hint.
///
/// Behind an interface so the cross-site policy comes from <see cref="AuthCookieOptions"/> instead of
/// being compiled in: the same binary must serve a same-site deployment (<c>SameSite=Lax</c>) and a
/// split-origin one (Vercel frontend + VPS API, which requires <c>SameSite=None; Secure</c>).
/// </summary>
public interface IAuthSessionCookie
{
    /// <summary>Writes the session cookie carrying <paramref name="token"/>.</summary>
    void Write(HttpContext context, string token, int maxAgeSeconds);

    /// <summary>Expires the session cookie (logout).</summary>
    void Clear(HttpContext context);
}

internal sealed class AuthSessionCookie : IAuthSessionCookie
{
    private readonly AuthCookieOptions _options;

    public AuthSessionCookie(IOptions<AuthCookieOptions> options) => _options = options.Value;

    public void Write(HttpContext context, string token, int maxAgeSeconds)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Response.Cookies.Append(AuthCookie.Name, token, BuildOptions(context, maxAgeSeconds));
    }

    public void Clear(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // The delete cookie MUST carry the same SameSite/Secure/Domain/Path attributes as the one it
        // replaces, or the browser treats it as a different cookie and the session survives logout.
        var options = BuildOptions(context, maxAgeSeconds: 1);
        options.MaxAge = TimeSpan.Zero;
        options.Expires = DateTimeOffset.UnixEpoch;
        context.Response.Cookies.Append(AuthCookie.Name, string.Empty, options);
    }

    private CookieOptions BuildOptions(HttpContext context, int maxAgeSeconds)
    {
        var options = new CookieOptions
        {
            HttpOnly = true,
            // Forced when configured, rather than derived from Request.IsHttps: the container listens
            // on plain HTTP behind a TLS-terminating proxy, so IsHttps depends on X-Forwarded-Proto
            // surviving the hop. A single missing proxy header would otherwise drop `Secure`, and a
            // SameSite=None cookie without Secure is discarded by the browser entirely.
            Secure = _options.AlwaysSecure || context.Request.IsHttps,
            SameSite = _options.ResolvedSameSite,
            Path = "/",
            MaxAge = TimeSpan.FromSeconds(Math.Max(1, maxAgeSeconds)),
            IsEssential = true,
        };

        if (!string.IsNullOrWhiteSpace(_options.Domain))
        {
            options.Domain = _options.Domain;
        }

        // CHIPS: gives the cross-site cookie a per-top-level-site jar.
        if (_options.Partitioned)
        {
            options.Extensions.Add("Partitioned");
        }

        return options;
    }
}
