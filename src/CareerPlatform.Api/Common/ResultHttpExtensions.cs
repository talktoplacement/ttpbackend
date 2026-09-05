using Microsoft.AspNetCore.Mvc;

namespace CareerPlatform.Api.Common;

/// <summary>
/// The single Result-to-<see cref="IResult"/> translator. Successful results render as a
/// 2xx response (with the value as the body when one is present, Req 6.6); failed results
/// render as an RFC 7807 <see cref="ProblemDetails"/> whose HTTP status is looked up from
/// the single-source <see cref="StatusMap"/> (Req 4.3, 4.4, 6.5). Per-field validation
/// errors are projected into the ProblemDetails <c>errors</c> extension (Req 5.5).
/// </summary>
public static class ResultHttpExtensions
{
    /// <summary>
    /// The single source of truth mapping every <see cref="ErrorType"/> to exactly one HTTP
    /// status code (Req 6.5). Every member of the closed <see cref="ErrorType"/> set has an
    /// entry, so the mapping is total (Req 4.4, 6.4).
    /// </summary>
    public static readonly IReadOnlyDictionary<ErrorType, int> StatusMap =
        new Dictionary<ErrorType, int>
        {
            [ErrorType.Validation]   = StatusCodes.Status400BadRequest,
            [ErrorType.Unauthorized] = StatusCodes.Status401Unauthorized,
            [ErrorType.Forbidden]    = StatusCodes.Status403Forbidden,
            [ErrorType.NotFound]     = StatusCodes.Status404NotFound,
            [ErrorType.Conflict]     = StatusCodes.Status409Conflict,
            [ErrorType.Failure]      = StatusCodes.Status500InternalServerError,
        };

    /// <summary>
    /// Translates a value-carrying <see cref="Result{T}"/> into an <see cref="IResult"/>:
    /// <see cref="Results.Ok(object?)"/> with the value on success (Req 6.6), or a
    /// ProblemDetails on failure.
    /// </summary>
    public static IResult ToHttpResult<T>(this Result<T> r) =>
        r.IsSuccess
            ? Results.Ok(r.Value)
            : ToProblem(r.Error);

    /// <summary>
    /// Translates a value-less <see cref="Result"/> into an <see cref="IResult"/>:
    /// <see cref="Results.Ok()"/> on success, or a ProblemDetails on failure.
    /// </summary>
    public static IResult ToHttpResult(this Result r) =>
        r.IsSuccess ? Results.Ok() : ToProblem(r.Error);

    /// <summary>
    /// Builds an RFC 7807 <see cref="ProblemDetails"/> from an <see cref="Error"/>, using the
    /// deterministic <see cref="StatusMap"/> for the status, the error code as the title, and
    /// the message as the detail. When the error carries field-level errors, they are grouped
    /// by field name into a <c>string[]</c> of messages under the <c>errors</c> extension
    /// (Req 4.3, 4.4, 5.5).
    /// </summary>
    private static IResult ToProblem(Error error)
    {
        var status = StatusMap[error.Type];
        var problem = new ProblemDetails
        {
            Status = status,
            Title = error.Code,
            Detail = error.Message,
        };

        if (error.FieldErrors is { Count: > 0 } fieldErrors)
        {
            problem.Extensions["errors"] = fieldErrors
                .GroupBy(f => f.Field)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Message).ToArray());
        }

        return Results.Problem(problem);
    }
}
