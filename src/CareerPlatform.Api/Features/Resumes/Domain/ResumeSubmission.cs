using System.ComponentModel.DataAnnotations;
using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.Resumes.Domain;

/// <summary>
/// A student's resume record. Content is stored via <c>IFileStorage</c> — this row keeps the
/// metadata + storage key. AtsScore is populated by an admin/analyzer flow when available.
/// </summary>
public sealed class ResumeSubmission : AuditableEntity<int>
{
    [Required] public string UserId { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string Title { get; set; } = string.Empty;
    /// <summary>Template code chosen from the <see cref="ResumeTemplate"/> catalog.</summary>
    [Required, MaxLength(64)] public string TemplateCode { get; set; } = string.Empty;
    /// <summary>Opaque storage key returned by IFileStorage; empty for placeholders.</summary>
    [MaxLength(500)] public string StorageKey { get; set; } = string.Empty;
    /// <summary>Optional ATS parse score (0..100) computed by a background pipeline.</summary>
    public int? AtsScore { get; set; }
}
