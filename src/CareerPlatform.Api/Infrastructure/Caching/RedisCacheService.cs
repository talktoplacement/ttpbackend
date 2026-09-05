using StackExchange.Redis;

namespace CareerPlatform.Api.Infrastructure.Caching;

/// <summary>
/// <see cref="ICacheService"/> backed by StackExchange.Redis. Values are JSON-serialized on
/// write and deserialized on read, so callers work with their own types (Req 17.1, 17.2).
/// </summary>
public sealed class RedisCacheService(IConnectionMultiplexer multiplexer) : ICacheService
{
    private readonly IConnectionMultiplexer _multiplexer = multiplexer;

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ct.ThrowIfCancellationRequested();

        var db = _multiplexer.GetDatabase();
        var value = await db.StringGetAsync(key);

        if (value.IsNullOrEmpty)
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>((string)value!);
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ct.ThrowIfCancellationRequested();

        var db = _multiplexer.GetDatabase();
        var payload = JsonSerializer.Serialize(value);
        await db.StringSetAsync(key, payload, ttl);
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ct.ThrowIfCancellationRequested();

        var db = _multiplexer.GetDatabase();
        await db.KeyDeleteAsync(key);
    }
}
