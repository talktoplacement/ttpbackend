using CareerPlatform.Api.Features.Cms.Dto;

namespace CareerPlatform.Api.Features.Cms.Service;

/// <summary>Admin CRUD + public read for header announcement banners.</summary>
public interface ICmsBannerService
{
    Task<Result<IReadOnlyList<CmsBannerResponse>>> ListAsync(bool activeOnly, CancellationToken ct);
    Task<Result<CmsBannerResponse>> GetAsync(int id, CancellationToken ct);
    Task<Result<CmsBannerResponse>> CreateAsync(UpsertCmsBannerRequest request, CancellationToken ct);
    Task<Result<CmsBannerResponse>> UpdateAsync(int id, UpsertCmsBannerRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
}
