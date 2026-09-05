using CareerPlatform.Api.Features.PlacementCompanies.Domain;
using CareerPlatform.Api.Features.PlacementCompanies.Dto;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.PlacementCompanies.Service;

/// <summary>Placement-companies workflow. Ports the 5 legacy MediatR handlers verbatim.</summary>
internal sealed class PlacementCompanyService : IPlacementCompanyService
{
    private readonly AppDbContext _db;
    public PlacementCompanyService(AppDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<PlacementCompanyResponse>>> ListAsync(
        string? tier, bool publishedOnly, CancellationToken ct)
    {
        var query = _db.PlacementCompanies.AsNoTracking();
        if (publishedOnly) query = query.Where(c => c.IsPublished);
        if (!string.IsNullOrWhiteSpace(tier))
        {
            var t = tier.Trim();
            query = query.Where(c => c.Tier == t);
        }
        var rows = await query
            .OrderByDescending(c => c.OpenPositions).ThenBy(c => c.Name)
            .ToListAsync(ct);
        IReadOnlyList<PlacementCompanyResponse> items = rows.Select(PlacementCompanyResponse.From).ToList();
        return Result.Success(items);
    }

    public async Task<Result<PlacementCompanyResponse>> GetAsync(string slug, CancellationToken ct)
    {
        var s = slug.Trim();
        var c = await _db.PlacementCompanies.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == s && x.IsPublished, ct);
        if (c is null)
        {
            return Result.Failure<PlacementCompanyResponse>(Error.NotFound(
                "PlacementCompany.NotFound", $"Company '{s}' was not found."));
        }
        return Result.Success(PlacementCompanyResponse.From(c));
    }

    public async Task<Result<PlacementCompanyResponse>> CreateAsync(CreatePlacementCompanyRequest r, CancellationToken ct)
    {
        var slug = r.Slug.Trim();
        var duplicate = await _db.PlacementCompanies.AnyAsync(c => c.Slug == slug, ct);
        if (duplicate)
        {
            return Result.Failure<PlacementCompanyResponse>(Error.Validation(
                "PlacementCompany.SlugExists", $"A company with slug '{slug}' already exists."));
        }
        var roles = r.HiringRoles is null ? string.Empty :
            string.Join(", ", r.HiringRoles.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
        var c = new PlacementCompany
        {
            Slug = slug,
            Name = r.Name.Trim(),
            Logo = r.Logo?.Trim() ?? string.Empty,
            Tier = r.Tier,
            CtcRange = r.CtcRange?.Trim() ?? string.Empty,
            HiringRoles = roles,
            OpenPositions = r.OpenPositions,
            Description = r.Description?.Trim() ?? string.Empty,
            IsPublished = r.IsPublished,
        };
        _db.PlacementCompanies.Add(c);
        await _db.SaveChangesAsync(ct);
        return Result.Success(PlacementCompanyResponse.From(c));
    }

    public async Task<Result<PlacementCompanyResponse>> UpdateAsync(int id, UpdatePlacementCompanyRequest r, CancellationToken ct)
    {
        var c = await _db.PlacementCompanies.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null)
        {
            return Result.Failure<PlacementCompanyResponse>(Error.NotFound(
                "PlacementCompany.NotFound", $"Company {id} was not found."));
        }
        if (r.Slug is not null)
        {
            var slug = r.Slug.Trim();
            if (slug != c.Slug)
            {
                var dup = await _db.PlacementCompanies.AnyAsync(x => x.Slug == slug && x.Id != id, ct);
                if (dup)
                {
                    return Result.Failure<PlacementCompanyResponse>(Error.Validation(
                        "PlacementCompany.SlugExists", $"A different company already uses slug '{slug}'."));
                }
                c.Slug = slug;
            }
        }
        if (r.Name is not null) c.Name = r.Name.Trim();
        if (r.Logo is not null) c.Logo = r.Logo.Trim();
        if (r.Tier is not null) c.Tier = r.Tier;
        if (r.CtcRange is not null) c.CtcRange = r.CtcRange.Trim();
        if (r.HiringRoles is not null)
        {
            c.HiringRoles = string.Join(", ",
                r.HiringRoles.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
        }
        if (r.OpenPositions is not null) c.OpenPositions = r.OpenPositions.Value;
        if (r.Description is not null) c.Description = r.Description;
        if (r.IsPublished is not null) c.IsPublished = r.IsPublished.Value;
        await _db.SaveChangesAsync(ct);
        return Result.Success(PlacementCompanyResponse.From(c));
    }

    public async Task<Result<PlacementCompanyResponse>> GetByIdAsync(int id, CancellationToken ct)
    {
        var c = await _db.PlacementCompanies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null)
        {
            return Result.Failure<PlacementCompanyResponse>(Error.NotFound(
                "PlacementCompany.NotFound", $"Company {id} was not found."));
        }
        return Result.Success(PlacementCompanyResponse.From(c));
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var c = await _db.PlacementCompanies.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null)
        {
            return Result.Failure(Error.NotFound(
                "PlacementCompany.NotFound", $"Company {id} was not found."));
        }
        _db.PlacementCompanies.Remove(c);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
