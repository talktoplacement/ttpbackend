using CareerPlatform.Api.Features.CourseCategories.Domain;
using CareerPlatform.Api.Features.CourseCategories.Dto;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.CourseCategories.Service;

internal sealed class CourseCategoryService : ICourseCategoryService
{
    private readonly AppDbContext _db;
    public CourseCategoryService(AppDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<CourseCategoryResponse>>> ListPublishedAsync(CancellationToken ct)
    {
        var rows = await _db.CourseCategories.AsNoTracking()
            .Where(c => c.IsPublished)
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .Take(PaginationRequest.MaxPageSize).ToListAsync(ct);
        return Result.Success((IReadOnlyList<CourseCategoryResponse>)rows.Select(CourseCategoryResponse.From).ToList());
    }

    public async Task<Result<IReadOnlyList<CourseCategoryResponse>>> ListAllAsync(CancellationToken ct)
    {
        var rows = await _db.CourseCategories.AsNoTracking()
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .Take(PaginationRequest.MaxPageSize).ToListAsync(ct);
        return Result.Success((IReadOnlyList<CourseCategoryResponse>)rows.Select(CourseCategoryResponse.From).ToList());
    }

    public async Task<Result<CourseCategoryResponse>> GetAsync(int id, CancellationToken ct)
    {
        var c = await _db.CourseCategories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null)
            return Result.Failure<CourseCategoryResponse>(Error.NotFound(
                "Category.NotFound", $"Category {id} was not found."));
        return Result.Success(CourseCategoryResponse.From(c));
    }

    public async Task<Result<CourseCategoryResponse>> CreateAsync(CreateCourseCategoryRequest r, CancellationToken ct)
    {
        var slug = r.Slug.Trim().ToLowerInvariant();
        if (await _db.CourseCategories.AnyAsync(c => c.Slug == slug, ct))
        {
            return Result.Failure<CourseCategoryResponse>(Error.Validation(
                "Category.SlugExists", $"A category with slug '{slug}' already exists."));
        }
        var cat = new CourseCategory
        {
            Slug = slug,
            Name = r.Name.Trim(),
            Description = r.Description?.Trim(),
            DisplayOrder = r.DisplayOrder,
            IsPublished = r.IsPublished,
        };
        _db.CourseCategories.Add(cat);
        await _db.SaveChangesAsync(ct);
        return Result.Success(CourseCategoryResponse.From(cat));
    }

    public async Task<Result<CourseCategoryResponse>> UpdateAsync(int id, UpdateCourseCategoryRequest r, CancellationToken ct)
    {
        var cat = await _db.CourseCategories.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (cat is null)
            return Result.Failure<CourseCategoryResponse>(Error.NotFound("Category.NotFound", $"Category {id} was not found."));
        cat.Name = r.Name.Trim();
        cat.Description = r.Description?.Trim();
        cat.DisplayOrder = r.DisplayOrder;
        cat.IsPublished = r.IsPublished;
        await _db.SaveChangesAsync(ct);
        return Result.Success(CourseCategoryResponse.From(cat));
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var cat = await _db.CourseCategories.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (cat is null)
            return Result.Failure(Error.NotFound("Category.NotFound", $"Category {id} was not found."));
        _db.CourseCategories.Remove(cat);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
