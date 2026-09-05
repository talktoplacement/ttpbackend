using CareerPlatform.Api.Features.PlacementCompanies.Dto;

namespace CareerPlatform.Api.Features.PlacementCompanies.Service;

public interface IPlacementCompanyService
{
    Task<Result<IReadOnlyList<PlacementCompanyResponse>>> ListAsync(string? tier, bool publishedOnly, CancellationToken ct);
    Task<Result<PlacementCompanyResponse>> GetAsync(string slug, CancellationToken ct);
    Task<Result<PlacementCompanyResponse>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<PlacementCompanyResponse>> CreateAsync(CreatePlacementCompanyRequest request, CancellationToken ct);
    Task<Result<PlacementCompanyResponse>> UpdateAsync(int id, UpdatePlacementCompanyRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
}
