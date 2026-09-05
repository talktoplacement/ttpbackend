namespace CareerPlatform.Api.Common;

/// <summary>
/// The uniform success/failure contract returned by every handler. A success carries
/// <see cref="Common.Error.None"/>; a failure carries exactly one real <see cref="Error"/>
/// with a non-empty code and a message of 1..500 characters. These invariants are enforced
/// in the constructor (Req 6.2, 6.3).
/// </summary>
public class Result : IResultBase
{
    /// <inheritdoc />
    public bool IsSuccess { get; }

    /// <inheritdoc />
    public bool IsFailure => !IsSuccess;

    /// <inheritdoc />
    public Error Error { get; }

    /// <summary>
    /// Constructs a result, enforcing the success/failure invariants:
    /// a success must carry <see cref="Common.Error.None"/>, and a failure must carry a
    /// real error with a non-empty code and a message of 1..500 characters.
    /// </summary>
    protected Result(bool isSuccess, Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        if (isSuccess)
        {
            if (error != Error.None)
            {
                throw new InvalidOperationException(
                    "A successful result cannot carry an error.");
            }
        }
        else
        {
            if (error == Error.None)
            {
                throw new InvalidOperationException(
                    "A failed result must carry a non-None error.");
            }

            if (string.IsNullOrEmpty(error.Code))
            {
                throw new InvalidOperationException(
                    "A failure error must have a non-empty code.");
            }

            if (error.Message.Length is < 1 or > 500)
            {
                throw new InvalidOperationException(
                    "A failure error message must be between 1 and 500 characters.");
            }
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>Creates a successful, value-less result.</summary>
    public static Result Success() => new(true, Error.None);

    /// <summary>Creates a failed, value-less result.</summary>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>Creates a successful result carrying <paramref name="value"/>.</summary>
    public static Result<T> Success<T>(T value) => new(value, true, Error.None);

    /// <summary>Creates a failed result of value type <typeparamref name="T"/>.</summary>
    public static Result<T> Failure<T>(Error error) => new(default, false, error);
}
