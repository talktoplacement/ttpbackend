using CareerPlatform.Api.Configuration;

namespace CareerPlatform.Api.Infrastructure.Storage;

/// <summary>
/// <see cref="IFileStorage"/> that persists blobs on the local filesystem under the configured
/// <see cref="StorageOptions.LocalPath"/> root (Req 17.1, 17.2). Logical paths are resolved
/// relative to that root; the resolved path is constrained to stay within the root.
/// </summary>
public sealed class LocalFileStorage(IOptions<StorageOptions> options) : IFileStorage
{
    private readonly string _root = ResolveRoot(options.Value);

    /// <inheritdoc />
    public async Task<string> SaveAsync(Stream content, string path, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrEmpty(path);

        var fullPath = ResolvePath(path);
        var directory = System.IO.Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var target = new FileStream(
            fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(target, ct);

        return path;
    }

    /// <inheritdoc />
    public Task<Stream> OpenAsync(string path, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ct.ThrowIfCancellationRequested();

        var fullPath = ResolvePath(path);
        Stream stream = new FileStream(
            fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(stream);
    }

    /// <inheritdoc />
    public Task DeleteAsync(string path, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(path)) return Task.CompletedTask;
        var fullPath = ResolvePath(path);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        return Task.CompletedTask;
    }

    private static string ResolveRoot(StorageOptions options)
    {
        var configured = string.IsNullOrWhiteSpace(options.LocalPath)
            ? System.IO.Path.Combine(AppContext.BaseDirectory, "storage")
            : options.LocalPath;

        return System.IO.Path.GetFullPath(configured);
    }

    private string ResolvePath(string path)
    {
        var combined = System.IO.Path.GetFullPath(System.IO.Path.Combine(_root, path));

        if (!combined.StartsWith(_root, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Resolved storage path '{combined}' escapes the configured root '{_root}'.");
        }

        return combined;
    }
}
