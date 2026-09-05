using System.Security.Cryptography;
using System.Text;
using Npgsql;

namespace CareerPlatform.Api.Infrastructure.Concurrency;

/// <summary>
/// <see cref="IDistributedLock"/> backed by PostgreSQL session-level advisory locks
/// (<c>pg_try_advisory_lock</c>).
///
/// Postgres is chosen over Redis because the database is a hard dependency that is always
/// configured, whereas Redis is optional in this deployment (<c>ConnectionStrings:Redis</c> may be
/// "disabled"). An advisory lock is held for the lifetime of the owning *connection*, so the handle
/// keeps a dedicated connection open and explicitly unlocks on dispose; if the process dies the
/// connection drops and Postgres releases the lock automatically — no stale locks, no TTL tuning.
/// </summary>
public sealed class PostgresAdvisoryLock : IDistributedLock
{
    private readonly string _connectionString;
    private readonly ILogger<PostgresAdvisoryLock> _logger;

    public PostgresAdvisoryLock(IConfiguration configuration, ILogger<PostgresAdvisoryLock> logger)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        _logger = logger;
    }

    public async Task<IAsyncDisposable?> TryAcquireAsync(string name, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            // No database configured (e.g. an isolated test host). Report "not acquired" so callers
            // skip the work rather than running it unguarded.
            _logger.LogDebug(
                "Advisory lock {Lock} skipped: no DefaultConnection configured.", name);
            return null;
        }

        var key = DeriveKey(name);
        var connection = new NpgsqlConnection(_connectionString);
        try
        {
            await connection.OpenAsync(ct);

            // Parameterized — the lock key is a bound bigint, never string-concatenated.
            await using var command =
                new NpgsqlCommand("SELECT pg_try_advisory_lock(@key)", connection);
            command.Parameters.AddWithValue("key", key);

            var acquired = await command.ExecuteScalarAsync(ct) as bool? ?? false;
            if (!acquired)
            {
                await connection.DisposeAsync();
                _logger.LogDebug(
                    "Advisory lock {Lock} is held by another instance; skipping this tick.", name);
                return null;
            }

            return new Handle(connection, key, name, _logger);
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    /// <summary>
    /// Maps a lock name to a stable 64-bit key. A cryptographic digest is used rather than
    /// <see cref="string.GetHashCode()"/> because .NET randomizes string hashing per process, which
    /// would give each replica a different key and defeat the lock entirely.
    /// </summary>
    private static long DeriveKey(string name)
    {
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(name));
        return BitConverter.ToInt64(digest, 0);
    }

    private sealed class Handle : IAsyncDisposable
    {
        private readonly NpgsqlConnection _connection;
        private readonly long _key;
        private readonly string _name;
        private readonly ILogger _logger;
        private bool _disposed;

        public Handle(NpgsqlConnection connection, long key, string name, ILogger logger)
        {
            _connection = connection;
            _key = key;
            _name = name;
            _logger = logger;
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                await using var command =
                    new NpgsqlCommand("SELECT pg_advisory_unlock(@key)", _connection);
                command.Parameters.AddWithValue("key", _key);
                await command.ExecuteScalarAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                // Closing the connection below releases the lock regardless, so this is not fatal.
                _logger.LogDebug(ex, "Explicit unlock of advisory lock {Lock} failed.", _name);
            }
            finally
            {
                await _connection.DisposeAsync();
            }
        }
    }
}
