namespace CareerPlatform.Api.Common;

/// <summary>
/// A structured error value carried by a failed <see cref="Result"/>. Every error has a
/// non-empty <paramref name="Code"/>, a human-readable <paramref name="Message"/> (1..500
/// characters when attached to a failure), a single <see cref="ErrorType"/> category, and
/// an optional set of field-level errors (Req 6.3, 6.4).
/// </summary>
/// <param name="Code">A non-empty, machine-readable error code (e.g. "Offer.NotFound").</param>
/// <param name="Message">A human-readable description of the failure.</param>
/// <param name="Type">The closed-set category of the error.</param>
/// <param name="FieldErrors">Optional per-field errors, used for validation failures.</param>
public sealed record Error(
    string Code,
    string Message,
    ErrorType Type,
    IReadOnlyList<FieldError>? FieldErrors = null)
{
    /// <summary>
    /// The sentinel "no error" value carried by every successful <see cref="Result"/>
    /// (Req 6.2). It is the only <see cref="Error"/> permitted on a success result.
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    /// <summary>Creates a <see cref="ErrorType.NotFound"/> error.</summary>
    public static Error NotFound(string code, string message) =>
        new(code, message, ErrorType.NotFound);

    /// <summary>
    /// Creates a <see cref="ErrorType.Validation"/> error, optionally carrying per-field
    /// errors that the HTTP translator renders into the ProblemDetails <c>errors</c> map.
    /// </summary>
    public static Error Validation(
        string code, string message, IReadOnlyList<FieldError>? fieldErrors = null) =>
        new(code, message, ErrorType.Validation, fieldErrors);

    /// <summary>Creates a <see cref="ErrorType.Conflict"/> error.</summary>
    public static Error Conflict(string code, string message) =>
        new(code, message, ErrorType.Conflict);

    /// <summary>Creates a <see cref="ErrorType.Unauthorized"/> error.</summary>
    public static Error Unauthorized(string code, string message) =>
        new(code, message, ErrorType.Unauthorized);

    /// <summary>Creates a <see cref="ErrorType.Forbidden"/> error.</summary>
    public static Error Forbidden(string code, string message) =>
        new(code, message, ErrorType.Forbidden);

    /// <summary>Creates a generic <see cref="ErrorType.Failure"/> error.</summary>
    public static Error Failure(string code, string message) =>
        new(code, message, ErrorType.Failure);
}
