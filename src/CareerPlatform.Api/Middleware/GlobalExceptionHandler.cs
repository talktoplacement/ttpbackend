using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CareerPlatform.Api.Middleware;

/// <summary>
/// Centralized <see cref="IExceptionHandler"/> for otherwise-unhandled exceptions. Converts any
/// downstream throw into an RFC 7807 <see cref="ProblemDetails"/> response (HTTP 500,
/// <c>application/problem+json</c>) carrying the request correlation id, while logging the full
/// exception (type, message, stack trace) server-side under that same correlation id.
/// </summary>
/// <remarks>
/// Outside the Development environment the <c>detail</c> member is a fixed generic message and no
/// exception type, raw message, or stack trace is exposed to the client. In Development the
/// exception message and stack trace are included to aid debugging (Req 7.1–7.6).
/// </remarks>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    /// <summary>Fixed, generic RFC 7807 <c>type</c> URI surfaced to clients.</summary>
    public const string ProblemType = "about:blank";

    /// <summary>Fixed, generic <c>title</c> surfaced to clients (Req 7.1, 7.4).</summary>
    public const string GenericTitle = "An unexpected error occurred.";

    /// <summary>Fixed, generic <c>detail</c> surfaced to clients outside Development (Req 7.4).</summary>
    public const string GenericDetail =
        "The request could not be completed. Please contact support with the correlation id.";

    /// <summary>The <c>correlationId</c> ProblemDetails extension member name (Req 7.2, 7.3).</summary>
    public const string CorrelationIdExtensionKey = "correlationId";

    /// <summary>The response content type for RFC 7807 problem responses.</summary>
    public const string ProblemContentType = "application/problem+json";

    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Reuse the correlation id assigned by CorrelationIdMiddleware; generate one as a fallback
        // if the exception surfaced before that middleware ran (Req 7.2, 7.3).
        var correlationId = httpContext.GetCorrelationId() ?? Guid.NewGuid().ToString("N");

        // Full server-side detail (type + message + stack trace) logged under the same correlation
        // id returned to the client (Req 7.6). Passing the exception logs its full stack trace.
        _logger.LogError(
            exception,
            "Unhandled exception {ExceptionType}: {Message}. CorrelationId={CorrelationId}",
            exception.GetType().FullName,
            exception.Message,
            correlationId);

        var isDevelopment = _environment.IsDevelopment();

        var problem = new ProblemDetails
        {
            Type = ProblemType,
            Title = GenericTitle,
            Status = StatusCodes.Status500InternalServerError,
            // In Development expose the exception message and stack trace; otherwise a fixed
            // generic message that leaks no exception type/message/stack trace (Req 7.4, 7.5).
            Detail = isDevelopment
                ? $"{exception.Message}\n{exception.StackTrace}"
                : GenericDetail,
        };
        problem.Extensions[CorrelationIdExtensionKey] = correlationId;

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(
            problem,
            options: null,
            contentType: ProblemContentType,
            cancellationToken: cancellationToken);

        return true;
    }
}
