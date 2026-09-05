using System.ComponentModel.DataAnnotations;
using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.Resumes.Domain;

/// <summary>
/// Admin-managed catalog of resume templates the frontend renders in its template picker.
/// </summary>
public sealed class ResumeTemplate : AuditableEntity<int>
{
    [Required, MaxLength(64)] public string Code { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
    [MaxLength(2000)] public string Description { get; set; } = string.Empty;
    [MaxLength(500)] public string PreviewUrl { get; set; } = string.Empty;
    public bool IsPublished { get; set; } = true;
}
