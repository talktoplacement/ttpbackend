namespace CareerPlatform.Api.Infrastructure;

/// <summary>
/// A distributed cache abstraction over the underlying store (Redis in the default
/// registration). Values are serialized/deserialized by the adapter, so callers work with
/// their own types and never touch the transport encoding (Req 17.1, 17.3).
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Reads the cached value stored under <paramref name="key"/>, or <c>null</c> when the
    /// key is absent or expired.
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken ct);

    /// <summary>
    /// Writes <paramref name="value"/> under <paramref name="key"/> with the given
    /// time-to-live, replacing any existing entry.
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct);

    /// <summary>Removes the entry stored under <paramref name="key"/>, if any.</summary>
    Task RemoveAsync(string key, CancellationToken ct);
}
