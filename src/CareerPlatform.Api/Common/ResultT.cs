namespace CareerPlatform.Api.Common;

/// <summary>
/// A <see cref="Result"/> that carries a value on success. Accessing <see cref="Value"/>
/// on a failed result throws, so callers must branch on <see cref="Result.IsSuccess"/>
/// before reading the value (Req 6.2, 6.3).
/// </summary>
/// <typeparam name="T">The type of the value produced on success.</typeparam>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    /// <summary>
    /// The success value. Throws <see cref="InvalidOperationException"/> when accessed on
    /// a failed result, because a failure carries no value.
    /// </summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access the value of a failed result.");

    internal Result(T? value, bool isSuccess, Error error) : base(isSuccess, error) =>
        _value = value;
}
