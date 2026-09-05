namespace CareerPlatform.Api.Infrastructure;

/// <summary>
/// A blob/file storage abstraction. The default registration writes to the local filesystem
/// under a configured root; an S3-compatible adapter (Cloudflare R2, MinIO, AWS S3) can be
/// substituted with a registration-only change based on <c>Storage:Provider</c>.
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Persists <paramref name="content"/> at the logical <paramref name="path"/> and returns
    /// a storage identifier/path that can later be passed to <see cref="OpenAsync"/>.
    /// </summary>
    Task<string> SaveAsync(Stream content, string path, CancellationToken ct);

    /// <summary>Opens a readable stream for the blob stored at <paramref name="path"/>.</summary>
    Task<Stream> OpenAsync(string path, CancellationToken ct);

    /// <summary>
    /// Removes the blob at <paramref name="path"/>. A missing key is treated as success so
    /// callers can call this idempotently when replacing an existing upload.
    /// </summary>
    Task DeleteAsync(string path, CancellationToken ct);
}
