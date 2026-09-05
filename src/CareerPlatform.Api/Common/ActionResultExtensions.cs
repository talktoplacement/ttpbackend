using Microsoft.AspNetCore.Mvc;

namespace CareerPlatform.Api.Common;

/// <summary>
/// MVC-controller companion to <see cref="ResultHttpExtensions"/>. Converts a
/// <see cref="Result"/> / <see cref="Result{T}"/> into an <see cref="ActionResult"/> so
/// controller actions can end with <c>return result.ToActionResult();</c>. Success maps to
/// <c>200 OK</c> (or <c>204 No Content</c> for the value-less form); failure maps to an
/// RFC 7807 <see cref="ProblemDetails"/> whose HTTP status is looked up from the single-source
/// <see cref="ResultHttpExtensions.StatusMap"/>.
/// </summary>
public static class ActionResultExtensions
{
    /// <summary>Value-carrying result → 200 with body on success, ProblemDetails on failure.</summary>
    public static ActionResult<T> ToActionResult<T>(this Result<T> r) =>
        r.IsSuccess
            ? new OkObjectResult(r.Value)
            : ToProblem<T>(r.Error);

    /// <summary>Value-less result → 204 on success, ProblemDetails on failure.</summary>
    public static ActionResult ToActionResult(this Result r) =>
        r.IsSuccess ? new NoContentResult() : ToProblem(r.Error);

    private static ActionResult<T> ToProblem<T>(Error error) =>
        new ObjectResult(BuildProblem(error)) { StatusCode = BuildProblem(error).Status };

    private static ActionResult ToProblem(Error error) =>
        new ObjectResult(BuildProblem(error)) { StatusCode = BuildProblem(error).Status };

    private static ProblemDetails BuildProblem(Error error)
    {
        var status = ResultHttpExtensions.StatusMap[error.Type];
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
        return problem;
    }
}
