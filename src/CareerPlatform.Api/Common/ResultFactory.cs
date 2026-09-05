using System.Collections.Concurrent;
using System.Reflection;

namespace CareerPlatform.Api.Common;

/// <summary>
/// Constructs a failure result of an arbitrary <c>TResponse</c> — either the non-generic
/// <see cref="Result"/> or a closed <see cref="Result{T}"/> — from an <see cref="Error"/>.
/// The validation pipeline uses this to short-circuit generically without knowing whether the
/// dispatched request produces a value (Req 5.4, 5.5).
/// </summary>
/// <remarks>
/// For <see cref="Result{T}"/> responses the concrete <c>Result.Failure&lt;T&gt;(Error)</c>
/// method is resolved once per closed type and cached as a compiled delegate, so repeated
/// dispatches pay the reflection cost only on first use.
/// </remarks>
public static class ResultFactory
{
    private static readonly ConcurrentDictionary<Type, Func<Error, object>> FailureFactories = new();

    private static readonly MethodInfo GenericFailureMethod =
        typeof(Result).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m is { Name: nameof(Result.Failure), IsGenericMethodDefinition: true });

    /// <summary>
    /// Builds a failure <typeparamref name="TResponse"/> carrying <paramref name="error"/>.
    /// Supports <see cref="Result"/> and any <see cref="Result{T}"/>; any other response type
    /// is a programming error and throws.
    /// </summary>
    public static TResponse Failure<TResponse>(Error error)
        where TResponse : IResultBase
    {
        ArgumentNullException.ThrowIfNull(error);

        var responseType = typeof(TResponse);

        // Non-generic Result.
        if (responseType == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(error);
        }

        // Closed Result<T>.
        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var factory = FailureFactories.GetOrAdd(responseType, BuildGenericFailureFactory);
            return (TResponse)factory(error);
        }

        throw new InvalidOperationException(
            $"ResultFactory cannot construct a failure for response type '{responseType}'. " +
            "Only Result and Result<T> are supported.");
    }

    private static Func<Error, object> BuildGenericFailureFactory(Type resultType)
    {
        var valueType = resultType.GetGenericArguments()[0];
        var closedMethod = GenericFailureMethod.MakeGenericMethod(valueType);
        return error => closedMethod.Invoke(null, [error])!;
    }
}
