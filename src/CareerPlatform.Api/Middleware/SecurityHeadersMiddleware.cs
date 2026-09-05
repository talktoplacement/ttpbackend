namespace CareerPlatform.Api.Middleware;

/// <summary>
/// The set of security response headers applied to every response. Ships with hardened defaults
/// and exposes an <see cref="Additional"/> map so callers can add or override headers without code
/// changes.
/// </summary>
/// <remarks>Requirement 14.3.</remarks>
public sealed class SecurityHeadersOptions
{
    public string XContentTypeOptions { get; set; } = "nosniff";

    public string XFrameOptions { get; set; } = "DENY";

    public string ReferrerPolicy { get; set; } = "no-referrer";

    /// <summary>Extra headers to apply, or overrides for the named defaults above.</summary>
    public IDictionary<string, string> Additional { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Materialises the full header set, with <see cref="Additional"/> taking precedence.</summary>
    public IReadOnlyDictionary<string, string> Build()
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["X-Content-Type-Options"] = XContentTypeOptions,
            ["X-Frame-Options"] = XFrameOptions,
            ["Referrer-Policy"] = ReferrerPolicy,
        };

        foreach (var (key, value) in Additional)
        {
            headers[key] = value;
        }

        return headers;
    }
}

/// <summary>
/// Adds the configured set of security headers to every response — including error responses —
/// using <see cref="HttpResponse.OnStarting(Func{Task})"/> so the headers are present before the
/// body is produced and regardless of the code path taken.
/// </summary>
/// <remarks>Requirement 14.3.</remarks>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IReadOnlyDictionary<string, string> _headers;

    public SecurityHeadersMiddleware(RequestDelegate next, SecurityHeadersOptions options)
    {
        _next = next;
        _headers = options.Build();
    }

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            foreach (var (name, value) in _headers)
            {
                context.Response.Headers[name] = value;
            }

            return Task.CompletedTask;
        });

        return _next(context);
    }
}

/// <summary>Extension helpers for the security-headers middleware.</summary>
public static class SecurityHeadersMiddlewareExtensions
{
    /// <summary>
    /// Adds the <see cref="SecurityHeadersMiddleware"/> to the request pipeline with an optional
    /// configuration callback for customising the applied header set.
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(
        this IApplicationBuilder app,
        Action<SecurityHeadersOptions>? configure = null)
    {
        var options = new SecurityHeadersOptions();
        configure?.Invoke(options);
        return app.UseMiddleware<SecurityHeadersMiddleware>(options);
    }
}
