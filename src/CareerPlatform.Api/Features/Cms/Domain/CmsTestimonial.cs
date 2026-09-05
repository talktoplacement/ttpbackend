using System.ComponentModel.DataAnnotations;
using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.Cms.Domain;

/// <summary>Marketing testimonial rendered on landing and pricing pages.</summary>
public sealed class CmsTestimonial : AuditableEntity<int>
{
    [Required, MaxLength(128)] public string AuthorName { get; set; } = string.Empty;

    [MaxLength(200)] public string? AuthorRole { get; set; }

    [Required, MaxLength(2000)] public string Quote { get; set; } = string.Empty;

    [MaxLength(500)] public string? AvatarUrl { get; set; }

    /// <summary>Optional 1..5 star rating shown next to the quote.</summary>
    public int? Rating { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsPublished { get; set; } = true;
}
