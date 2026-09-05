using System.ComponentModel.DataAnnotations;
using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.Cms.Domain;

/// <summary>
/// Singleton configuration for the public landing hero + CTAs. Exactly one row is expected
/// (<see cref="SingletonId"/>); the service upserts it so there is never a hardcoded homepage.
/// </summary>
public sealed class CmsHomepageConfig : AuditableEntity<int>
{
    /// <summary>The fixed primary key for the single config row.</summary>
    public const int SingletonId = 1;

    [Required, MaxLength(200)]
    public string HeroTitle { get; set; } = string.Empty;

    [MaxLength(500)]
    public string HeroSubtitle { get; set; } = string.Empty;

    [MaxLength(64)]
    public string PrimaryCtaLabel { get; set; } = string.Empty;

    [MaxLength(500)]
    public string PrimaryCtaHref { get; set; } = string.Empty;

    [MaxLength(64)]
    public string SecondaryCtaLabel { get; set; } = string.Empty;

    [MaxLength(500)]
    public string SecondaryCtaHref { get; set; } = string.Empty;
}
