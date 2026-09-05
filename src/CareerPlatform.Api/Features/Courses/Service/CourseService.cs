using CareerPlatform.Api.Configuration;
using CareerPlatform.Api.Features.Courses.Domain;
using CareerPlatform.Api.Features.Courses.Dto;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.Courses.Service;

/// <summary>
/// Business logic for the Courses feature. Ports the six legacy MediatR handlers verbatim:
/// <c>ListPublishedCoursesHandler</c>, <c>ListCoursesHandler</c>, <c>GetCourseHandler</c>,
/// <c>CreateCourseHandler</c>, <c>UpdateCourseHandler</c>, <c>DeleteCourseHandler</c>. All EF
/// queries stay parameterized and async. Never leaks the entity; every return uses
/// <see cref="CourseResponse"/>.
/// </summary>
internal sealed class CourseService : ICourseService
{
    private const string CourseProductType = "Course";

    private readonly AppDbContext _db;
    private readonly SubscriptionOptions _options;

    public CourseService(AppDbContext db, IOptions<SubscriptionOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public async Task<Result<IReadOnlyList<CourseResponse>>> ListPublishedAsync(CancellationToken ct)
    {
        var courses = await _db.Courses
            .Where(c => c.IsPublished)
            .OrderBy(c => c.Price)
            .ThenBy(c => c.Id)
            .Take(PaginationRequest.MaxPageSize)
            .ToListAsync(ct);

        IReadOnlyList<CourseResponse> items = courses.Select(CourseResponse.From).ToList();
        return Result.Success(items);
    }

    public async Task<Result<PaginatedResult<CourseResponse>>> ListAllAsync(
        int? page, int? pageSize, CancellationToken ct)
    {
        var pagination = new PaginationRequest(page, pageSize);
        var effectivePage = pagination.EffectivePage;
        var effectiveSize = Math.Min(pagination.EffectivePageSize, _options.CatalogMaxPageSize);

        var total = await _db.Courses.LongCountAsync(ct);

        var courses = await _db.Courses
            .OrderBy(c => c.Id)
            .Skip((effectivePage - 1) * effectiveSize)
            .Take(effectiveSize)
            .ToListAsync(ct);

        IReadOnlyList<CourseResponse> items = courses.Select(CourseResponse.From).ToList();
        return Result.Success(
            PaginatedResult<CourseResponse>.Create(items, effectivePage, effectiveSize, total));
    }

    public async Task<Result<CourseResponse>> GetByIdAsync(int id, CancellationToken ct)
    {
        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (course is null)
        {
            return Result.Failure<CourseResponse>(Error.NotFound(
                "Course.NotFound", $"Course {id} was not found."));
        }
        return Result.Success(CourseResponse.From(course));
    }

    public async Task<Result<CourseResponse>> CreateAsync(CreateCourseRequest request, CancellationToken ct)
    {
        var slugExists = await _db.Courses.AnyAsync(c => c.Slug == request.Slug, ct);
        if (slugExists)
        {
            return Result.Failure<CourseResponse>(Error.Validation(
                "Course.SlugExists", $"A course with slug '{request.Slug}' already exists."));
        }

        var course = new Course
        {
            Slug = request.Slug,
            Title = request.Title,
            Description = request.Description ?? string.Empty,
            Price = request.Price,
            MediaUrl = request.MediaUrl,
            IsPublished = request.IsPublished,
        };

        _db.Courses.Add(course);
        await _db.SaveChangesAsync(ct);

        return Result.Success(CourseResponse.From(course));
    }

    public async Task<Result<CourseResponse>> UpdateAsync(int id, UpdateCourseRequest request, CancellationToken ct)
    {
        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (course is null)
        {
            return Result.Failure<CourseResponse>(Error.NotFound(
                "Course.NotFound", $"Course {id} was not found."));
        }

        var slugOwnedByAnother =
            await _db.Courses.AnyAsync(c => c.Slug == request.Slug && c.Id != id, ct);
        if (slugOwnedByAnother)
        {
            return Result.Failure<CourseResponse>(Error.Validation(
                "Course.SlugExists",
                $"A different course already uses slug '{request.Slug}'."));
        }

        course.Slug = request.Slug;
        course.Title = request.Title;
        course.Description = request.Description ?? string.Empty;
        course.Price = request.Price;
        course.MediaUrl = request.MediaUrl;
        course.IsPublished = request.IsPublished;

        await _db.SaveChangesAsync(ct);

        return Result.Success(CourseResponse.From(course));
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (course is null)
        {
            return Result.Failure(Error.NotFound(
                "Course.NotFound", $"Course {id} was not found."));
        }

        var referenced =
            await _db.Orders.AnyAsync(
                o => o.ProductType == CourseProductType && o.ProductId == id, ct)
            || await _db.Enrollments.AnyAsync(
                e => e.ProductType == CourseProductType && e.ProductId == id, ct);

        if (referenced)
        {
            course.IsPublished = false;
        }
        else
        {
            _db.Courses.Remove(course);
        }

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
