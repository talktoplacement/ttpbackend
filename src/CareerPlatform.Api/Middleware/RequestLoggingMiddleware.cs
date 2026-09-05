using System.Diagnostics;

namespace CareerPlatform.Api.Middleware;

/// <summary>
/// Measures the elapsed time of each request with a <see cref="Stopwatch"/> and, after the
/// downstream pipeline completes, emits a single structured log entry containing the HTTP method,
/// route/path, status code, and elapsed milliseconds.
/// </summary>
/// <remarks>Requirement 13.2.</remarks>
public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            // Prefer the matched route pattern when available; fall back to the raw path.
            var route = context.GetEndpoint()?.DisplayName
                ?? context.Request.Path.Value
                ?? string.Empty;

            _logger.LogInformation(
                "HTTP {RequestMethod} {RequestRoute} responded {StatusCode} in {ElapsedMilliseconds} ms",
                context.Request.Method,
                route,
                context.Response.StatusCode,
                stopwatch.Elapsed.TotalMilliseconds);
        }
    }
}

/// <summary>Extension helpers for the request-logging middleware.</summary>
public static class RequestLoggingMiddlewareExtensions
{
    /// <summary>Adds the <see cref="RequestLoggingMiddleware"/> to the request pipeline.</summary>
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
        => app.UseMiddleware<RequestLoggingMiddleware>();
}
