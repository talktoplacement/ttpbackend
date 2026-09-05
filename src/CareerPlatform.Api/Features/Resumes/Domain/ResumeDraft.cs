using System.ComponentModel.DataAnnotations;
using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.Resumes.Domain;

/// <summary>
/// An in-progress resume the student is still editing, distinct from
/// <see cref="ResumeSubmission"/> (a finished artifact submitted for review).
///
/// <see cref="ContentJson"/> is stored opaquely on purpose. The builder document's shape is owned by
/// the frontend, and modelling each section as a column would mean a schema change every time a
/// section is added. The server therefore validates size and well-formedness, not structure — the
/// alternative was the previous behaviour, where the draft lived only in a client-side store and was
/// lost on remount.
/// </summary>
public sealed class ResumeDraft : AuditableEntity<int>
{
    [Required, MaxLength(64)]
    public string UserId { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>References <see cref="ResumeTemplate.Code"/>; validated against the catalog on write.</summary>
    [Required, MaxLength(64)]
    public string TemplateCode { get; set; } = string.Empty;

    [Required]
    public string ContentJson { get; set; } = "{}";

    public DateTime LastEditedAtUtc { get; set; } = DateTime.UtcNow;
}
