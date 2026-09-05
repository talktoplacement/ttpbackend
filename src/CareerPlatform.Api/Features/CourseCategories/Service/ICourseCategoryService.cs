using CareerPlatform.Api.Features.CourseCategories.Dto;

namespace CareerPlatform.Api.Features.CourseCategories.Service;

public interface ICourseCategoryService
{
    Task<Result<IReadOnlyList<CourseCategoryResponse>>> ListPublishedAsync(CancellationToken ct);
    Task<Result<IReadOnlyList<CourseCategoryResponse>>> ListAllAsync(CancellationToken ct);
    Task<Result<CourseCategoryResponse>> GetAsync(int id, CancellationToken ct);
    Task<Result<CourseCategoryResponse>> CreateAsync(CreateCourseCategoryRequest request, CancellationToken ct);
    Task<Result<CourseCategoryResponse>> UpdateAsync(int id, UpdateCourseCategoryRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
}
