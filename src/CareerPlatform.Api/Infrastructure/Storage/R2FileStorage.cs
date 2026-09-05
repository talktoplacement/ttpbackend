using Amazon.S3;
using Amazon.S3.Model;
using CareerPlatform.Api.Configuration;

namespace CareerPlatform.Api.Infrastructure.Storage;

/// <summary>
/// <see cref="IFileStorage"/> backed by an S3-compatible object store. Configured for
/// Cloudflare R2 by pointing <see cref="StorageOptions.Endpoint"/> at the account's R2 URL,
/// but works with plain AWS S3 or MinIO by adjusting the same options. Uses path-style URLs
/// because R2 does not support virtual-hosted-style addressing.
///
/// Object retention (auto-delete after 30 days) should be configured as an R2 bucket
/// lifecycle rule out-of-band. This adapter additionally exposes <see cref="DeleteAsync"/>
/// so the application can eagerly remove replaced or expired objects.
/// </summary>
public sealed class R2FileStorage : IFileStorage
{
    private readonly IAmazonS3 _client;
    private readonly string _bucket;

    public R2FileStorage(IOptions<StorageOptions> options)
    {
        var value = options.Value;
        if (string.IsNullOrWhiteSpace(value.BucketName))
        {
            throw new InvalidOperationException("Storage:BucketName is required for R2/S3 storage.");
        }
        if (string.IsNullOrWhiteSpace(value.Endpoint))
        {
            throw new InvalidOperationException("Storage:Endpoint is required for R2/S3 storage.");
        }
        if (string.IsNullOrWhiteSpace(value.AccessKeyId) || string.IsNullOrWhiteSpace(value.SecretAccessKey))
        {
            throw new InvalidOperationException(
                "Storage:AccessKeyId and Storage:SecretAccessKey are required for R2/S3 storage.");
        }

        var config = new AmazonS3Config
        {
            ServiceURL = value.Endpoint,
            ForcePathStyle = true,
            // R2 ignores the region but the SDK requires a non-null value.
            AuthenticationRegion = string.IsNullOrWhiteSpace(value.Region) ? "auto" : value.Region,
        };

        _client = new AmazonS3Client(value.AccessKeyId, value.SecretAccessKey, config);
        _bucket = value.BucketName;
    }

    public async Task<string> SaveAsync(Stream content, string path, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrEmpty(path);

        var request = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = path,
            InputStream = content,
            AutoCloseStream = false,
            DisablePayloadSigning = true, // Required by R2 (does not support chunked signing).
        };
        await _client.PutObjectAsync(request, ct).ConfigureAwait(false);
        return path;
    }

    public async Task<Stream> OpenAsync(string path, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        var request = new GetObjectRequest { BucketName = _bucket, Key = path };
        var response = await _client.GetObjectAsync(request, ct).ConfigureAwait(false);
        return response.ResponseStream;
    }

    /// <summary>Removes the object at <paramref name="path"/>. No-op if the key is missing.</summary>
    public async Task DeleteAsync(string path, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        try
        {
            await _client
                .DeleteObjectAsync(new DeleteObjectRequest { BucketName = _bucket, Key = path }, ct)
                .ConfigureAwait(false);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Already gone; treat as success.
        }
    }
}
