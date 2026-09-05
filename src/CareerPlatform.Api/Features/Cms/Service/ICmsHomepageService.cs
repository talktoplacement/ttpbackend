using CareerPlatform.Api.Features.Cms.Dto;

namespace CareerPlatform.Api.Features.Cms.Service;

/// <summary>Read + upsert for the singleton homepage hero/CTA configuration.</summary>
public interface ICmsHomepageService
{
    Task<Result<CmsHomepageResponse>> GetAsync(CancellationToken ct);
    Task<Result<CmsHomepageResponse>> UpdateAsync(UpdateCmsHomepageRequest request, CancellationToken ct);
}
