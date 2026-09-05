using System.ComponentModel.DataAnnotations;
using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.Cms.Domain;

/// <summary>A single FAQ entry rendered on landing / pricing pages.</summary>
public sealed class CmsFaq : AuditableEntity<int>
{
    [Required, MaxLength(500)] public string Question { get; set; } = string.Empty;

    [Required, MaxLength(4000)] public string Answer { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    public bool IsPublished { get; set; } = true;
}
