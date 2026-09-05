using CareerPlatform.Api.Features.Cms.Domain;
using CareerPlatform.Api.Features.Cms.Dto;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.Cms.Service;

internal sealed class CmsHomepageService : ICmsHomepageService
{
    private readonly AppDbContext _db;
    public CmsHomepageService(AppDbContext db) => _db = db;

    public async Task<Result<CmsHomepageResponse>> GetAsync(CancellationToken ct)
    {
        var config = await _db.CmsHomepageConfigs.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == CmsHomepageConfig.SingletonId, ct);
        // No row yet: return an empty (but valid) shape rather than 404 so the editor can render.
        return Result.Success(config is null
            ? new CmsHomepageResponse("", "", "", "", "", "")
            : CmsHomepageResponse.From(config));
    }

    public async Task<Result<CmsHomepageResponse>> UpdateAsync(UpdateCmsHomepageRequest r, CancellationToken ct)
    {
        var config = await _db.CmsHomepageConfigs
            .FirstOrDefaultAsync(c => c.Id == CmsHomepageConfig.SingletonId, ct);
        if (config is null)
        {
            config = new CmsHomepageConfig { Id = CmsHomepageConfig.SingletonId };
            _db.CmsHomepageConfigs.Add(config);
        }
        config.HeroTitle = r.HeroTitle.Trim();
        config.HeroSubtitle = r.HeroSubtitle?.Trim() ?? string.Empty;
        config.PrimaryCtaLabel = r.PrimaryCtaLabel?.Trim() ?? string.Empty;
        config.PrimaryCtaHref = r.PrimaryCtaHref?.Trim() ?? string.Empty;
        config.SecondaryCtaLabel = r.SecondaryCtaLabel?.Trim() ?? string.Empty;
        config.SecondaryCtaHref = r.SecondaryCtaHref?.Trim() ?? string.Empty;
        await _db.SaveChangesAsync(ct);
        return Result.Success(CmsHomepageResponse.From(config));
    }
}
