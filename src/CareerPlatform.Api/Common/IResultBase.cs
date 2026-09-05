namespace CareerPlatform.Api.Common;

/// <summary>
/// Non-generic marker contract implemented by both <see cref="Result"/> and
/// <see cref="Result{T}"/>. It lets the mediator validation pipeline constrain
/// <c>TResponse</c> to a result type without knowing the concrete value type.
/// </summary>
public interface IResultBase
{
    /// <summary>True when the operation succeeded.</summary>
    bool IsSuccess { get; }

    /// <summary>True when the operation failed.</summary>
    bool IsFailure { get; }

    /// <summary>
    /// The failure error. <see cref="Common.Error.None"/> on success.
    /// </summary>
    Error Error { get; }
}
