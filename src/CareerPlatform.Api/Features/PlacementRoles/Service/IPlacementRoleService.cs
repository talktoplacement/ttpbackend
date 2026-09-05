using CareerPlatform.Api.Features.PlacementRoles.Dto;

namespace CareerPlatform.Api.Features.PlacementRoles.Service;

public interface IPlacementRoleService
{
    Task<Result<IReadOnlyList<PlacementRoleResponse>>> ListPublishedAsync(CancellationToken ct);
    Task<Result<PlacementRoleResponse>> GetBySlugAsync(string slug, CancellationToken ct);
    Task<Result<IReadOnlyList<PlacementRoleResponse>>> ListAllAsync(CancellationToken ct);

    /// <summary>Admin: fetch by surrogate id (includes unpublished, unlike the public slug read).</summary>
    Task<Result<PlacementRoleResponse>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<PlacementRoleResponse>> CreateAsync(CreatePlacementRoleRequest request, CancellationToken ct);
    Task<Result<PlacementRoleResponse>> UpdateAsync(int id, UpdatePlacementRoleRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
}
