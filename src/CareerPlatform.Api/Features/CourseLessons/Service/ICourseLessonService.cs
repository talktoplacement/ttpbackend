using CareerPlatform.Api.Features.CourseLessons.Dto;

namespace CareerPlatform.Api.Features.CourseLessons.Service;

public interface ICourseLessonService
{
    /// <summary>Student: published lessons for a course joined with the caller's progress.</summary>
    Task<Result<CourseLessonsWithProgressResponse>> ListForStudentAsync(int courseId, CancellationToken ct);

    /// <summary>Admin: every lesson (published or not) for a course.</summary>
    Task<Result<IReadOnlyList<CourseLessonResponse>>> ListForAdminAsync(int courseId, CancellationToken ct);

    Task<Result<CourseLessonResponse>> CreateAsync(int courseId, CreateCourseLessonRequest request, CancellationToken ct);
    Task<Result<CourseLessonResponse>> UpdateAsync(int courseId, int lessonId, UpdateCourseLessonRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int courseId, int lessonId, CancellationToken ct);
    Task<Result> ReorderAsync(int courseId, ReorderCourseLessonsRequest request, CancellationToken ct);
}
