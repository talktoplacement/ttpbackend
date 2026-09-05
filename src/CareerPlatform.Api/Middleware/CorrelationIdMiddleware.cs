using Serilog.Context;

namespace CareerPlatform.Api.Middleware;

/// <summary>
/// Reads the incoming <c>X-Correlation-Id</c> request header (using it when present and 1..128
/// characters long) or generates a new id, stores it on <see cref="HttpContext.Items"/>, pushes it
/// to the Serilog <see cref="LogContext"/> so every downstream log entry carries it, and writes it
/// to the response header on every code path — including error responses — via
/// <see cref="HttpResponse.OnStarting(Func{Task})"/>.
/// </summary>
/// <remarks>Requirements 12.1, 12.2, 12.3, 12.4, 12.5, 12.6, 14.5.</remarks>
public sealed class CorrelationIdMiddleware
{
    /// <summary>The request/response header carrying the correlation id.</summary>
    public const string HeaderName = "X-Correlation-Id";

    /// <summary>The <see cref="HttpContext.Items"/> key under which the id is stored.</summary>
    public const string ItemsKey = "CorrelationId";

    /// <summary>The Serilog <see cref="LogContext"/> property name for the id.</summary>
    public const string LogPropertyName = "CorrelationId";

    private const int MaxLength = 128;

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);

        // Make the id retrievable by downstream components (e.g. the exception handler).
        context.Items[ItemsKey] = correlationId;

        // Guarantee the header is written even when a downstream stage throws before writing a
        // body: OnStarting runs just before the response headers are flushed on every path.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        // Enrich every log entry emitted while this request is in flight (Req 12.4).
        using (LogContext.PushProperty(LogPropertyName, correlationId))
        {
            await _next(context);
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var provided))
        {
            var candidate = provided.ToString();
            if (candidate.Length is >= 1 and <= MaxLength)
            {
                return candidate;
            }
        }

        // GUID "N" format is 32 hex characters — within the 1..128 bound.
        return Guid.NewGuid().ToString("N");
    }
}

/// <summary>Extension helpers for the correlation-id middleware.</summary>
public static class CorrelationIdMiddlewareExtensions
{
    /// <summary>Adds the <see cref="CorrelationIdMiddleware"/> to the request pipeline.</summary>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        => app.UseMiddleware<CorrelationIdMiddleware>();

    /// <summary>
    /// Returns the correlation id assigned to the current request, or <c>null</c> if the
    /// <see cref="CorrelationIdMiddleware"/> has not yet run for this request.
    /// </summary>
    public static string? GetCorrelationId(this HttpContext context)
        => context.Items.TryGetValue(CorrelationIdMiddleware.ItemsKey, out var value)
            ? value as string
            : null;
}
