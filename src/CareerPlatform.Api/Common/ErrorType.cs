namespace CareerPlatform.Api.Common;

/// <summary>
/// The closed set of error categories a failed <see cref="Result"/> can carry.
/// Each category maps deterministically to exactly one HTTP status code by the
/// Result-to-IResult translator (see task 2.3). This set is intentionally closed:
/// adding a member is a deliberate contract change (Req 6.4).
/// </summary>
public enum ErrorType
{
    /// <summary>Input failed validation. Maps to HTTP 400.</summary>
    Validation,

    /// <summary>Authentication is missing or invalid. Maps to HTTP 401.</summary>
    Unauthorized,

    /// <summary>Authenticated but not permitted. Maps to HTTP 403.</summary>
    Forbidden,

    /// <summary>The target resource does not exist. Maps to HTTP 404.</summary>
    NotFound,

    /// <summary>The request conflicts with current state. Maps to HTTP 409.</summary>
    Conflict,

    /// <summary>A generic or unexpected domain failure. Maps to HTTP 500.</summary>
    Failure
}
