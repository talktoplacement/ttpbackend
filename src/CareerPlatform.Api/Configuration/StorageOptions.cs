namespace CareerPlatform.Api.Configuration;

/// <summary>
/// Strongly-typed binding for the <c>Storage</c> configuration section.
///
/// The Provider switch chooses between the local filesystem (dev) and an S3-compatible
/// object store (production). Cloudflare R2 is S3-compatible: point <see cref="Endpoint"/>
/// at your account's R2 endpoint (`https://&lt;accountId&gt;.r2.cloudflarestorage.com`), set
/// <see cref="BucketName"/> to the bucket, and supply the access-key pair. No R2-specific
/// SDK is required.
/// </summary>
public sealed class StorageOptions
{
    public const string Section = "Storage";

    /// <summary>Storage provider identifier. Recognised values: <c>Local</c>, <c>R2</c>, <c>S3</c>.</summary>
    public string Provider { get; init; } = "Local";

    /// <summary>Root path used by the local file-storage provider.</summary>
    public string? LocalPath { get; init; }

    /// <summary>Bucket name used by object-storage providers.</summary>
    public string? BucketName { get; init; }

    /// <summary>Region for object-storage providers (R2 accepts <c>auto</c>).</summary>
    public string? Region { get; init; }

    /// <summary>
    /// Endpoint URL for S3-compatible providers. Required for R2 (e.g.
    /// <c>https://&lt;accountId&gt;.r2.cloudflarestorage.com</c>).
    /// </summary>
    public string? Endpoint { get; init; }

    /// <summary>Access key id for S3-compatible providers.</summary>
    public string? AccessKeyId { get; init; }

    /// <summary>Secret access key for S3-compatible providers.</summary>
    public string? SecretAccessKey { get; init; }
}
