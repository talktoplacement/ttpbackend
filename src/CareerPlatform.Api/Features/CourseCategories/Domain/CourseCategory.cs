using System.ComponentModel.DataAnnotations;
using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.CourseCategories.Domain;

/// <summary>Top-level course categorization used for catalog filtering + navigation.</summary>
public sealed class CourseCategory : AuditableEntity<int>
{
    [Required, MaxLength(64)] public string Slug { get; set; } = string.Empty;
    [Required, MaxLength(128)] public string Name { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPublished { get; set; } = true;
}
