namespace CareerPlatform.Api.Infrastructure.Concurrency;

/// <summary>
/// A cluster-wide mutual-exclusion primitive used to ensure that a periodic job body runs on
/// exactly one instance at a time, even when the API is horizontally scaled.
///
/// Without this, every replica's <see cref="BackgroundService"/> timer fires independently and the
/// same rows are processed concurrently — producing racing UPDATEs and duplicated side effects.
/// Implementations are expected to be non-blocking: acquisition either succeeds immediately or
/// returns <c>null</c> so the caller can skip this tick and try again on the next one.
/// </summary>
public interface IDistributedLock
{
    /// <summary>
    /// Attempts to acquire the named lock without waiting.
    /// </summary>
    /// <param name="name">
    /// Logical lock name. The same name must be used by every replica competing for the work.
    /// </param>
    /// <returns>
    /// A handle that releases the lock when disposed, or <c>null</c> when another instance
    /// currently holds it.
    /// </returns>
    Task<IAsyncDisposable?> TryAcquireAsync(string name, CancellationToken ct);
}
