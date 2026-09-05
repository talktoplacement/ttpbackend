using CareerPlatform.Api.Features.Cms.Domain;
using CareerPlatform.Api.Features.Cms.Dto;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.Cms.Service;

internal sealed class CmsService : ICmsService
{
    private readonly AppDbContext _db;
    public CmsService(AppDbContext db) => _db = db;

    // ── FAQs ────────────────────────────────────────────────────────────────

    public async Task<Result<IReadOnlyList<CmsFaqResponse>>> ListPublishedFaqsAsync(CancellationToken ct)
    {
        var rows = await _db.CmsFaqs.AsNoTracking()
            .Where(f => f.IsPublished)
            .OrderBy(f => f.DisplayOrder).ThenBy(f => f.Id)
            .Take(PaginationRequest.MaxPageSize).ToListAsync(ct);
        return Result.Success((IReadOnlyList<CmsFaqResponse>)rows.Select(CmsFaqResponse.From).ToList());
    }

    public async Task<Result<IReadOnlyList<CmsFaqResponse>>> ListAllFaqsAsync(CancellationToken ct)
    {
        var rows = await _db.CmsFaqs.AsNoTracking()
            .OrderBy(f => f.DisplayOrder).ThenBy(f => f.Id)
            .Take(PaginationRequest.MaxPageSize).ToListAsync(ct);
        return Result.Success((IReadOnlyList<CmsFaqResponse>)rows.Select(CmsFaqResponse.From).ToList());
    }

    public async Task<Result<CmsFaqResponse>> GetFaqAsync(int id, CancellationToken ct)
    {
        var f = await _db.CmsFaqs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (f is null)
            return Result.Failure<CmsFaqResponse>(Error.NotFound("Faq.NotFound", $"FAQ {id} was not found."));
        return Result.Success(CmsFaqResponse.From(f));
    }

    public async Task<Result<CmsFaqResponse>> CreateFaqAsync(UpsertCmsFaqRequest r, CancellationToken ct)
    {
        var faq = new CmsFaq
        {
            Question = r.Question.Trim(),
            Answer = r.Answer.Trim(),
            DisplayOrder = r.DisplayOrder,
            IsPublished = r.IsPublished,
        };
        _db.CmsFaqs.Add(faq);
        await _db.SaveChangesAsync(ct);
        return Result.Success(CmsFaqResponse.From(faq));
    }

    public async Task<Result<CmsFaqResponse>> UpdateFaqAsync(int id, UpsertCmsFaqRequest r, CancellationToken ct)
    {
        var faq = await _db.CmsFaqs.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (faq is null)
            return Result.Failure<CmsFaqResponse>(Error.NotFound("Faq.NotFound", $"FAQ {id} was not found."));
        faq.Question = r.Question.Trim();
        faq.Answer = r.Answer.Trim();
        faq.DisplayOrder = r.DisplayOrder;
        faq.IsPublished = r.IsPublished;
        await _db.SaveChangesAsync(ct);
        return Result.Success(CmsFaqResponse.From(faq));
    }

    public async Task<Result> DeleteFaqAsync(int id, CancellationToken ct)
    {
        var faq = await _db.CmsFaqs.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (faq is null)
            return Result.Failure(Error.NotFound("Faq.NotFound", $"FAQ {id} was not found."));
        _db.CmsFaqs.Remove(faq);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ── Testimonials ────────────────────────────────────────────────────────

    public async Task<Result<IReadOnlyList<CmsTestimonialResponse>>> ListPublishedTestimonialsAsync(CancellationToken ct)
    {
        var rows = await _db.CmsTestimonials.AsNoTracking()
            .Where(t => t.IsPublished)
            .OrderBy(t => t.DisplayOrder).ThenBy(t => t.Id)
            .Take(PaginationRequest.MaxPageSize).ToListAsync(ct);
        return Result.Success((IReadOnlyList<CmsTestimonialResponse>)rows.Select(CmsTestimonialResponse.From).ToList());
    }

    public async Task<Result<IReadOnlyList<CmsTestimonialResponse>>> ListAllTestimonialsAsync(CancellationToken ct)
    {
        var rows = await _db.CmsTestimonials.AsNoTracking()
            .OrderBy(t => t.DisplayOrder).ThenBy(t => t.Id)
            .Take(PaginationRequest.MaxPageSize).ToListAsync(ct);
        return Result.Success((IReadOnlyList<CmsTestimonialResponse>)rows.Select(CmsTestimonialResponse.From).ToList());
    }

    public async Task<Result<CmsTestimonialResponse>> GetTestimonialAsync(int id, CancellationToken ct)
    {
        var t = await _db.CmsTestimonials.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null)
            return Result.Failure<CmsTestimonialResponse>(Error.NotFound(
                "Testimonial.NotFound", $"Testimonial {id} was not found."));
        return Result.Success(CmsTestimonialResponse.From(t));
    }

    public async Task<Result<CmsTestimonialResponse>> CreateTestimonialAsync(UpsertCmsTestimonialRequest r, CancellationToken ct)
    {
        var t = new CmsTestimonial
        {
            AuthorName = r.AuthorName.Trim(),
            AuthorRole = r.AuthorRole?.Trim(),
            Quote = r.Quote.Trim(),
            AvatarUrl = r.AvatarUrl?.Trim(),
            Rating = r.Rating,
            DisplayOrder = r.DisplayOrder,
            IsPublished = r.IsPublished,
        };
        _db.CmsTestimonials.Add(t);
        await _db.SaveChangesAsync(ct);
        return Result.Success(CmsTestimonialResponse.From(t));
    }

    public async Task<Result<CmsTestimonialResponse>> UpdateTestimonialAsync(int id, UpsertCmsTestimonialRequest r, CancellationToken ct)
    {
        var t = await _db.CmsTestimonials.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null)
            return Result.Failure<CmsTestimonialResponse>(Error.NotFound("Testimonial.NotFound", $"Testimonial {id} was not found."));
        t.AuthorName = r.AuthorName.Trim();
        t.AuthorRole = r.AuthorRole?.Trim();
        t.Quote = r.Quote.Trim();
        t.AvatarUrl = r.AvatarUrl?.Trim();
        t.Rating = r.Rating;
        t.DisplayOrder = r.DisplayOrder;
        t.IsPublished = r.IsPublished;
        await _db.SaveChangesAsync(ct);
        return Result.Success(CmsTestimonialResponse.From(t));
    }

    public async Task<Result> DeleteTestimonialAsync(int id, CancellationToken ct)
    {
        var t = await _db.CmsTestimonials.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null)
            return Result.Failure(Error.NotFound("Testimonial.NotFound", $"Testimonial {id} was not found."));
        _db.CmsTestimonials.Remove(t);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ── Navigation ──────────────────────────────────────────────────────────

    public async Task<Result<IReadOnlyList<CmsNavigationLinkResponse>>> ListPublishedNavigationAsync(string groupName, CancellationToken ct)
    {
        var g = string.IsNullOrWhiteSpace(groupName) ? "header" : groupName.Trim().ToLowerInvariant();
        var rows = await _db.CmsNavigationLinks.AsNoTracking()
            .Where(n => n.IsPublished && n.GroupName == g)
            .OrderBy(n => n.DisplayOrder).ThenBy(n => n.Id)
            .Take(PaginationRequest.MaxPageSize).ToListAsync(ct);
        return Result.Success((IReadOnlyList<CmsNavigationLinkResponse>)rows.Select(CmsNavigationLinkResponse.From).ToList());
    }

    public async Task<Result<IReadOnlyList<CmsNavigationLinkResponse>>> ListAllNavigationAsync(CancellationToken ct)
    {
        var rows = await _db.CmsNavigationLinks.AsNoTracking()
            .OrderBy(n => n.GroupName).ThenBy(n => n.DisplayOrder).ThenBy(n => n.Id)
            .Take(PaginationRequest.MaxPageSize).ToListAsync(ct);
        return Result.Success((IReadOnlyList<CmsNavigationLinkResponse>)rows.Select(CmsNavigationLinkResponse.From).ToList());
    }

    public async Task<Result<CmsNavigationLinkResponse>> GetNavigationAsync(int id, CancellationToken ct)
    {
        var n = await _db.CmsNavigationLinks.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (n is null)
            return Result.Failure<CmsNavigationLinkResponse>(Error.NotFound(
                "Navigation.NotFound", $"Navigation link {id} was not found."));
        return Result.Success(CmsNavigationLinkResponse.From(n));
    }

    public async Task<Result<CmsNavigationLinkResponse>> CreateNavigationAsync(UpsertCmsNavigationLinkRequest r, CancellationToken ct)
    {
        var n = new CmsNavigationLink
        {
            Label = r.Label.Trim(),
            Href = r.Href.Trim(),
            GroupName = r.GroupName.Trim().ToLowerInvariant(),
            IsExternal = r.IsExternal,
            DisplayOrder = r.DisplayOrder,
            IsPublished = r.IsPublished,
        };
        _db.CmsNavigationLinks.Add(n);
        await _db.SaveChangesAsync(ct);
        return Result.Success(CmsNavigationLinkResponse.From(n));
    }

    public async Task<Result<CmsNavigationLinkResponse>> UpdateNavigationAsync(int id, UpsertCmsNavigationLinkRequest r, CancellationToken ct)
    {
        var n = await _db.CmsNavigationLinks.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (n is null)
            return Result.Failure<CmsNavigationLinkResponse>(Error.NotFound("Navigation.NotFound", $"Navigation link {id} was not found."));
        n.Label = r.Label.Trim();
        n.Href = r.Href.Trim();
        n.GroupName = r.GroupName.Trim().ToLowerInvariant();
        n.IsExternal = r.IsExternal;
        n.DisplayOrder = r.DisplayOrder;
        n.IsPublished = r.IsPublished;
        await _db.SaveChangesAsync(ct);
        return Result.Success(CmsNavigationLinkResponse.From(n));
    }

    public async Task<Result> DeleteNavigationAsync(int id, CancellationToken ct)
    {
        var n = await _db.CmsNavigationLinks.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (n is null)
            return Result.Failure(Error.NotFound("Navigation.NotFound", $"Navigation link {id} was not found."));
        _db.CmsNavigationLinks.Remove(n);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
