using CareerPlatform.Api.Features.Cms.Domain;

namespace CareerPlatform.Api.Features.Cms.Dto;

// ── FAQs ─────────────────────────────────────────────────────────────────────
public sealed record CmsFaqResponse(
    int Id,
    string Question,
    string Answer,
    int DisplayOrder,
    bool IsPublished)
{
    public static CmsFaqResponse From(CmsFaq f)
    {
        ArgumentNullException.ThrowIfNull(f);
        return new CmsFaqResponse(f.Id, f.Question, f.Answer, f.DisplayOrder, f.IsPublished);
    }
}

public sealed record UpsertCmsFaqRequest(
    string Question,
    string Answer,
    int DisplayOrder = 0,
    bool IsPublished = true);

// ── Testimonials ─────────────────────────────────────────────────────────────
public sealed record CmsTestimonialResponse(
    int Id,
    string AuthorName,
    string? AuthorRole,
    string Quote,
    string? AvatarUrl,
    int? Rating,
    int DisplayOrder,
    bool IsPublished)
{
    public static CmsTestimonialResponse From(CmsTestimonial t)
    {
        ArgumentNullException.ThrowIfNull(t);
        return new CmsTestimonialResponse(
            t.Id, t.AuthorName, t.AuthorRole, t.Quote, t.AvatarUrl,
            t.Rating, t.DisplayOrder, t.IsPublished);
    }
}

public sealed record UpsertCmsTestimonialRequest(
    string AuthorName,
    string? AuthorRole,
    string Quote,
    string? AvatarUrl,
    int? Rating,
    int DisplayOrder = 0,
    bool IsPublished = true);

// ── Navigation ───────────────────────────────────────────────────────────────
public sealed record CmsNavigationLinkResponse(
    int Id,
    string Label,
    string Href,
    string GroupName,
    bool IsExternal,
    int DisplayOrder,
    bool IsPublished)
{
    public static CmsNavigationLinkResponse From(CmsNavigationLink n)
    {
        ArgumentNullException.ThrowIfNull(n);
        return new CmsNavigationLinkResponse(
            n.Id, n.Label, n.Href, n.GroupName, n.IsExternal, n.DisplayOrder, n.IsPublished);
    }
}

public sealed record UpsertCmsNavigationLinkRequest(
    string Label,
    string Href,
    string GroupName,
    bool IsExternal = false,
    int DisplayOrder = 0,
    bool IsPublished = true);
