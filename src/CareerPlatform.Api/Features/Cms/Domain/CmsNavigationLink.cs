using System.ComponentModel.DataAnnotations;
using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.Cms.Domain;

/// <summary>
/// A single link entry in the public site navigation (header or footer). Grouped by
/// <see cref="GroupName"/> so a page-layout component can render just the header or just the
/// footer set from one call.
/// </summary>
public sealed class CmsNavigationLink : AuditableEntity<int>
{
    [Required, MaxLength(64)] public string Label { get; set; } = string.Empty;

    [Required, MaxLength(500)] public string Href { get; set; } = string.Empty;

    /// <summary><c>header</c> / <c>footer</c>. Free-text so additional groups (e.g. mobile-drawer) can be added without a schema change.</summary>
    [Required, MaxLength(32)] public string GroupName { get; set; } = "header";

    /// <summary>When true the link opens in a new tab (rel=noopener/target=_blank).</summary>
    public bool IsExternal { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsPublished { get; set; } = true;
}
