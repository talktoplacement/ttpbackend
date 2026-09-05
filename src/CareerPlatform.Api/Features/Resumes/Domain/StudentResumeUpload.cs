using System.ComponentModel.DataAnnotations;
using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.Resumes.Domain;

/// <summary>
/// A student-uploaded PDF resume held in object storage (Cloudflare R2 in production). Only the
/// most recent upload per student is retained — the endpoint that ingests new uploads deletes
/// the older row and its blob before inserting.
///
/// Objects are additionally auto-purged 30 days after upload by a background service and by an
/// R2 bucket lifecycle rule (belt-and-braces).
/// </summary>
public sealed class StudentResumeUpload : AuditableEntity<int>
{
    /// <summary>Owner (subject id from the JWT).</summary>
    [Required]
    public string StudentUserId { get; set; } = string.Empty;

    /// <summary>Opaque object-storage key. Never rendered to the client.</summary>
    [Required, MaxLength(500)]
    public string StorageKey { get; set; } = string.Empty;

    /// <summary>Original filename the student uploaded (used for display + Content-Disposition).</summary>
    [Required, MaxLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>Size of the stored PDF in bytes (capped at 1 MB by the endpoint).</summary>
    public long SizeBytes { get; set; }

    /// <summary>Upload timestamp; also drives 30-day retention.</summary>
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Hard-deleted after this instant by the background purge job. Set to
    /// <c>UploadedAtUtc + 30d</c> at insert time.
    /// </summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>
    /// UserId of the mentor the resume has been mapped to. Nullable — an admin assigns the
    /// mentor after the upload, and reassignment simply overwrites this value.
    /// </summary>
    [MaxLength(64)]
    public string? AssignedMentorUserId { get; set; }

    /// <summary>Timestamp at which the current mentor assignment was made.</summary>
    public DateTime? AssignedAtUtc { get; set; }
}
