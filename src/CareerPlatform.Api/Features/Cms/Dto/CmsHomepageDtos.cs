using CareerPlatform.Api.Features.Cms.Domain;

namespace CareerPlatform.Api.Features.Cms.Dto;

public sealed record CmsHomepageResponse(
    string HeroTitle, string HeroSubtitle,
    string PrimaryCtaLabel, string PrimaryCtaHref,
    string SecondaryCtaLabel, string SecondaryCtaHref)
{
    public static CmsHomepageResponse From(CmsHomepageConfig c)
    {
        ArgumentNullException.ThrowIfNull(c);
        return new CmsHomepageResponse(
            c.HeroTitle, c.HeroSubtitle,
            c.PrimaryCtaLabel, c.PrimaryCtaHref,
            c.SecondaryCtaLabel, c.SecondaryCtaHref);
    }
}

/// <summary>Body for <c>PUT /api/v1/admin/cms/homepage</c> — full replace of the singleton config.</summary>
public sealed record UpdateCmsHomepageRequest(
    string HeroTitle, string? HeroSubtitle,
    string? PrimaryCtaLabel, string? PrimaryCtaHref,
    string? SecondaryCtaLabel, string? SecondaryCtaHref);
