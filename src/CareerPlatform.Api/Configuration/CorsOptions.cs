namespace CareerPlatform.Api.Configuration;

/// <summary>
/// Strongly-typed binding for the <c>Cors</c> configuration section (Req 15.1).
/// </summary>
public sealed class CorsOptions
{
    public const string Section = "Cors";

    /// <summary>Origins permitted to call the API. Empty by default (no cross-origin access).</summary>
    public string[] AllowedOrigins { get; init; } = [];

    /// <summary>
    /// Host suffixes whose HTTPS origins are also allowed, e.g. <c>-myteam.vercel.app</c>.
    ///
    /// Exists because a Vercel preview deployment gets a fresh hostname per commit
    /// (<c>app-git-branch-team.vercel.app</c>), which a literal origin list can never match — the
    /// alternative is redeploying the API for every preview branch. Matching is on the HOST only and
    /// still requires HTTPS.
    ///
    /// Keep entries as specific as possible: a bare <c>.vercel.app</c> would trust every app on that
    /// shared domain, so prefer the team-scoped suffix. Empty by default.
    /// </summary>
    public string[] AllowedOriginSuffixes { get; init; } = [];

    /// <summary>
    /// True when <paramref name="origin"/> is a well-formed absolute <c>https://</c> origin.
    /// The CORS policy grants credentials, so outside Development only secure origins are trusted —
    /// a plaintext origin would expose the session cookie to interception.
    /// </summary>
    public static bool IsSecureOrigin(string? origin) =>
        Uri.TryCreate(origin, UriKind.Absolute, out var uri)
        && uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// True when <paramref name="origin"/> is an HTTPS origin whose host ends with one of
    /// <see cref="AllowedOriginSuffixes"/>.
    /// </summary>
    public bool MatchesAllowedSuffix(string? origin)
    {
        if (AllowedOriginSuffixes.Length == 0) return false;
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)) return false;
        if (!uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)) return false;

        foreach (var suffix in AllowedOriginSuffixes)
        {
            if (IsUsableSuffix(suffix)
                && uri.Host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Rejects blank entries and anything with fewer than two labels, so a typo such as <c>.app</c>
    /// cannot open the API to an entire top-level domain.
    /// </summary>
    public static bool IsUsableSuffix(string? suffix) =>
        !string.IsNullOrWhiteSpace(suffix)
        && suffix.TrimStart('.').Count(c => c == '.') >= 1;
}
