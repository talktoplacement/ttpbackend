using CareerPlatform.Api.Features.PlacementRoles.Domain;
using CareerPlatform.Api.Features.PlacementRoles.Dto;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.PlacementRoles.Service;

internal sealed class PlacementRoleService : IPlacementRoleService
{
    private readonly AppDbContext _db;
    public PlacementRoleService(AppDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<PlacementRoleResponse>>> ListPublishedAsync(CancellationToken ct)
    {
        var rows = await _db.PlacementRoles.AsNoTracking()
            .Where(r => r.IsPublished)
            .OrderBy(r => r.Title)
            .Take(PaginationRequest.MaxPageSize).ToListAsync(ct);
        return Result.Success((IReadOnlyList<PlacementRoleResponse>)rows.Select(PlacementRoleResponse.From).ToList());
    }

    public async Task<Result<PlacementRoleResponse>> GetBySlugAsync(string slug, CancellationToken ct)
    {
        var s = slug.Trim().ToLowerInvariant();
        var row = await _db.PlacementRoles.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Slug == s && r.IsPublished, ct);
        if (row is null)
            return Result.Failure<PlacementRoleResponse>(Error.NotFound(
                "PlacementRole.NotFound", $"Placement role '{s}' was not found."));
        return Result.Success(PlacementRoleResponse.From(row));
    }

    public async Task<Result<IReadOnlyList<PlacementRoleResponse>>> ListAllAsync(CancellationToken ct)
    {
        var rows = await _db.PlacementRoles.AsNoTracking()
            .OrderBy(r => r.Title)
            .Take(PaginationRequest.MaxPageSize).ToListAsync(ct);
        return Result.Success((IReadOnlyList<PlacementRoleResponse>)rows.Select(PlacementRoleResponse.From).ToList());
    }

    public async Task<Result<PlacementRoleResponse>> GetByIdAsync(int id, CancellationToken ct)
    {
        var row = await _db.PlacementRoles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, ct);
        if (row is null)
            return Result.Failure<PlacementRoleResponse>(Error.NotFound(
                "PlacementRole.NotFound", $"Placement role {id} was not found."));
        return Result.Success(PlacementRoleResponse.From(row));
    }

    public async Task<Result<PlacementRoleResponse>> CreateAsync(CreatePlacementRoleRequest r, CancellationToken ct)
    {
        var slug = r.Slug.Trim().ToLowerInvariant();
        if (await _db.PlacementRoles.AnyAsync(x => x.Slug == slug, ct))
            return Result.Failure<PlacementRoleResponse>(Error.Validation(
                "PlacementRole.SlugExists", $"A placement role with slug '{slug}' already exists."));
        var role = new PlacementRole
        {
            Slug = slug,
            Title = r.Title.Trim(),
            AvgCtcRange = r.AvgCtcRange?.Trim(),
            RequirementsMarkdown = r.RequirementsMarkdown?.Trim(),
            IsPublished = r.IsPublished,
        };
        _db.PlacementRoles.Add(role);
        await _db.SaveChangesAsync(ct);
        return Result.Success(PlacementRoleResponse.From(role));
    }

    public async Task<Result<PlacementRoleResponse>> UpdateAsync(int id, UpdatePlacementRoleRequest r, CancellationToken ct)
    {
        var role = await _db.PlacementRoles.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (role is null)
            return Result.Failure<PlacementRoleResponse>(Error.NotFound(
                "PlacementRole.NotFound", $"Placement role {id} was not found."));
        role.Title = r.Title.Trim();
        role.AvgCtcRange = r.AvgCtcRange?.Trim();
        role.RequirementsMarkdown = r.RequirementsMarkdown?.Trim();
        role.IsPublished = r.IsPublished;
        await _db.SaveChangesAsync(ct);
        return Result.Success(PlacementRoleResponse.From(role));
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var role = await _db.PlacementRoles.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (role is null)
            return Result.Failure(Error.NotFound("PlacementRole.NotFound", $"Placement role {id} was not found."));
        _db.PlacementRoles.Remove(role);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
