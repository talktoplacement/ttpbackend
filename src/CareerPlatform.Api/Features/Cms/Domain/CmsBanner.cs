using System.ComponentModel.DataAnnotations;
using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.Cms.Domain;

/// <summary>A public announcement banner shown in the site header. Fully admin-managed.</summary>
public sealed class CmsBanner : AuditableEntity<int>
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    /// <summary>Optional call-to-action link.</summary>
    [MaxLength(500)]
    public string? LinkUrl { get; set; }

    /// <summary>Visual tone: info / success / warning / danger.</summary>
    [Required, MaxLength(16)]
    public string Tone { get; set; } = "info";

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
