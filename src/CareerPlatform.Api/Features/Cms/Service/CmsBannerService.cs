using CareerPlatform.Api.Features.Cms.Domain;
using CareerPlatform.Api.Features.Cms.Dto;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.Cms.Service;

internal sealed class CmsBannerService : ICmsBannerService
{
    private readonly AppDbContext _db;
    public CmsBannerService(AppDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<CmsBannerResponse>>> ListAsync(bool activeOnly, CancellationToken ct)
    {
        var query = _db.CmsBanners.AsNoTracking();
        if (activeOnly) query = query.Where(b => b.IsActive);
        var rows = await query
            .OrderBy(b => b.DisplayOrder).ThenByDescending(b => b.Id)
            .Take(PaginationRequest.MaxPageSize)
            .ToListAsync(ct);
        return Result.Success((IReadOnlyList<CmsBannerResponse>)rows.Select(CmsBannerResponse.From).ToList());
    }

    public async Task<Result<CmsBannerResponse>> GetAsync(int id, CancellationToken ct)
    {
        var b = await _db.CmsBanners.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (b is null)
        {
            return Result.Failure<CmsBannerResponse>(Error.NotFound(
                "CmsBanner.NotFound", $"Banner {id} was not found."));
        }
        return Result.Success(CmsBannerResponse.From(b));
    }

    public async Task<Result<CmsBannerResponse>> CreateAsync(UpsertCmsBannerRequest r, CancellationToken ct)
    {
        var banner = new CmsBanner
        {
            Title = r.Title.Trim(),
            Message = r.Message.Trim(),
            LinkUrl = string.IsNullOrWhiteSpace(r.LinkUrl) ? null : r.LinkUrl.Trim(),
            Tone = r.Tone,
            DisplayOrder = r.DisplayOrder,
            IsActive = r.IsActive,
        };
        _db.CmsBanners.Add(banner);
        await _db.SaveChangesAsync(ct);
        return Result.Success(CmsBannerResponse.From(banner));
    }

    public async Task<Result<CmsBannerResponse>> UpdateAsync(int id, UpsertCmsBannerRequest r, CancellationToken ct)
    {
        var banner = await _db.CmsBanners.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (banner is null)
        {
            return Result.Failure<CmsBannerResponse>(Error.NotFound(
                "CmsBanner.NotFound", $"Banner {id} was not found."));
        }
        banner.Title = r.Title.Trim();
        banner.Message = r.Message.Trim();
        banner.LinkUrl = string.IsNullOrWhiteSpace(r.LinkUrl) ? null : r.LinkUrl.Trim();
        banner.Tone = r.Tone;
        banner.DisplayOrder = r.DisplayOrder;
        banner.IsActive = r.IsActive;
        await _db.SaveChangesAsync(ct);
        return Result.Success(CmsBannerResponse.From(banner));
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var banner = await _db.CmsBanners.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (banner is null)
        {
            return Result.Failure(Error.NotFound(
                "CmsBanner.NotFound", $"Banner {id} was not found."));
        }
        _db.CmsBanners.Remove(banner);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
