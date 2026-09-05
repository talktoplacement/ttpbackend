using CareerPlatform.Api.Features.Cms.Domain;

namespace CareerPlatform.Api.Features.Cms.Dto;

public sealed record CmsBannerResponse(
    int Id, string Title, string Message, string? LinkUrl,
    string Tone, int DisplayOrder, bool IsActive)
{
    public static CmsBannerResponse From(CmsBanner b)
    {
        ArgumentNullException.ThrowIfNull(b);
        return new CmsBannerResponse(
            b.Id, b.Title, b.Message, b.LinkUrl, b.Tone, b.DisplayOrder, b.IsActive);
    }
}

/// <summary>Create/update body for a CMS banner. Used by both POST and PUT.</summary>
public sealed record UpsertCmsBannerRequest(
    string Title, string Message, string? LinkUrl,
    string Tone, int DisplayOrder, bool IsActive);
