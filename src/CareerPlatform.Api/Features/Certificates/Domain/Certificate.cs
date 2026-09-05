using System.ComponentModel.DataAnnotations;
using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.Certificates.Domain;

/// <summary>
/// Achievement certificate issued to a student. Rendered on the student's certificates
/// dashboard and downloadable as a PDF stored via <c>IFileStorage</c>. A public verification
/// endpoint resolves <see cref="VerificationCode"/> so third parties can confirm authenticity.
/// </summary>
public sealed class Certificate : AuditableEntity<int>
{
    [Required, MaxLength(64)] public string UserId { get; set; } = string.Empty;

    /// <summary>Human-readable certificate title (e.g. "DSA Mastery — Advanced").</summary>
    [Required, MaxLength(200)] public string Title { get; set; } = string.Empty;

    /// <summary>Optional context — the specific course, cohort, or achievement recognised.</summary>
    [MaxLength(200)] public string? IssuedFor { get; set; }

    public DateTime IssuedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Short opaque token embedded on the certificate. Used by the public verification page so
    /// employers can confirm the certificate is real without any authentication.
    /// </summary>
    [Required, MaxLength(64)] public string VerificationCode { get; set; } = string.Empty;

    /// <summary>Opaque storage key for the rendered PDF; empty until the file is available.</summary>
    [MaxLength(500)] public string? StorageKey { get; set; }

    /// <summary>When set, the certificate has been revoked — verification lookups must fail.</summary>
    public DateTime? RevokedAtUtc { get; set; }
}
