using CareerPlatform.Api.Features.Cms.Dto;

namespace CareerPlatform.Api.Features.Cms.Service;

/// <summary>
/// Aggregate service for the CMS bounded context (FAQs, testimonials, navigation links).
/// Kept as a single service because all three entities are simple CMS records with the same
/// admin CRUD shape — splitting into three interfaces would be indirection without polymorphism.
/// </summary>
public interface ICmsService
{
    // FAQs
    Task<Result<IReadOnlyList<CmsFaqResponse>>> ListPublishedFaqsAsync(CancellationToken ct);
    Task<Result<IReadOnlyList<CmsFaqResponse>>> ListAllFaqsAsync(CancellationToken ct);
    Task<Result<CmsFaqResponse>> GetFaqAsync(int id, CancellationToken ct);
    Task<Result<CmsFaqResponse>> CreateFaqAsync(UpsertCmsFaqRequest request, CancellationToken ct);
    Task<Result<CmsFaqResponse>> UpdateFaqAsync(int id, UpsertCmsFaqRequest request, CancellationToken ct);
    Task<Result> DeleteFaqAsync(int id, CancellationToken ct);

    // Testimonials
    Task<Result<IReadOnlyList<CmsTestimonialResponse>>> ListPublishedTestimonialsAsync(CancellationToken ct);
    Task<Result<IReadOnlyList<CmsTestimonialResponse>>> ListAllTestimonialsAsync(CancellationToken ct);
    Task<Result<CmsTestimonialResponse>> GetTestimonialAsync(int id, CancellationToken ct);
    Task<Result<CmsTestimonialResponse>> CreateTestimonialAsync(UpsertCmsTestimonialRequest request, CancellationToken ct);
    Task<Result<CmsTestimonialResponse>> UpdateTestimonialAsync(int id, UpsertCmsTestimonialRequest request, CancellationToken ct);
    Task<Result> DeleteTestimonialAsync(int id, CancellationToken ct);

    // Navigation
    /// <summary>Public: navigation links for the given group (e.g. <c>header</c>).</summary>
    Task<Result<IReadOnlyList<CmsNavigationLinkResponse>>> ListPublishedNavigationAsync(string groupName, CancellationToken ct);
    Task<Result<IReadOnlyList<CmsNavigationLinkResponse>>> ListAllNavigationAsync(CancellationToken ct);
    Task<Result<CmsNavigationLinkResponse>> GetNavigationAsync(int id, CancellationToken ct);
    Task<Result<CmsNavigationLinkResponse>> CreateNavigationAsync(UpsertCmsNavigationLinkRequest request, CancellationToken ct);
    Task<Result<CmsNavigationLinkResponse>> UpdateNavigationAsync(int id, UpsertCmsNavigationLinkRequest request, CancellationToken ct);
    Task<Result> DeleteNavigationAsync(int id, CancellationToken ct);
}
