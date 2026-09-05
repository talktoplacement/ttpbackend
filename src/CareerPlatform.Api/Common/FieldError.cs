namespace CareerPlatform.Api.Common;

/// <summary>
/// A single field-level error, used to convey per-field validation failures. The
/// Result-to-IResult translator renders these into the ProblemDetails <c>errors</c>
/// dictionary (Req 5.5).
/// </summary>
/// <param name="Field">The name of the field the error applies to.</param>
/// <param name="Message">The human-readable message describing the failure.</param>
public sealed record FieldError(string Field, string Message);
