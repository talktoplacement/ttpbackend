using CareerPlatform.Api.Features.Courses.Dto;

namespace CareerPlatform.Api.Features.Courses.Service;

/// <summary>
/// Public contract for the Courses feature. One method per business use case; consumed by
/// <see cref="Controller.CoursesController"/>. Every method returns a <see cref="Result"/> or
/// <see cref="Result{T}"/> so failures carry a code + message and translate to a stable HTTP
/// status via <see cref="Common.ActionResultExtensions"/>.
/// </summary>
public interface ICourseService
{
    /// <summary>Public catalog — every published course, price-ascending.</summary>
    Task<Result<IReadOnlyList<CourseResponse>>> ListPublishedAsync(CancellationToken ct);

    /// <summary>Admin listing — every course, paginated.</summary>
    Task<Result<PaginatedResult<CourseResponse>>> ListAllAsync(int? page, int? pageSize, CancellationToken ct);

    /// <summary>Admin single-course fetch by id; <c>NotFound</c> when absent.</summary>
    Task<Result<CourseResponse>> GetByIdAsync(int id, CancellationToken ct);

    /// <summary>Admin create; <c>Validation</c> failure on duplicate slug.</summary>
    Task<Result<CourseResponse>> CreateAsync(CreateCourseRequest request, CancellationToken ct);

    /// <summary>Admin update; <c>NotFound</c> when the id doesn't exist, <c>Validation</c> on slug conflict.</summary>
    Task<Result<CourseResponse>> UpdateAsync(int id, UpdateCourseRequest request, CancellationToken ct);

    /// <summary>
    /// Admin delete-or-archive. Physically removes the row when no order/enrollment references it;
    /// otherwise sets <c>IsPublished = false</c> to preserve referential integrity.
    /// </summary>
    Task<Result> DeleteAsync(int id, CancellationToken ct);
}
