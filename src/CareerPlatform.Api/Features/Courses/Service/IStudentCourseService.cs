using CareerPlatform.Api.Features.Courses.Dto;

namespace CareerPlatform.Api.Features.Courses.Service;

/// <summary>
/// The signed-in student's own course library.
///
/// Separate from <see cref="ICourseService"/> because that interface is the admin/catalog CRUD
/// surface and has no notion of a caller — every method here is scoped to the authenticated user, and
/// keeping the two apart means a catalog method can never accidentally leak or require identity.
/// </summary>
public interface IStudentCourseService
{
    /// <summary>
    /// Lists the courses the caller can access, each with their progress.
    ///
    /// Access is the union of an explicit per-course enrollment, an active paid subscription, and any
    /// course the student already has progress on. That union is what makes the list correct for both
    /// of today's grant models and for per-course purchases when they ship, without another rewrite.
    /// </summary>
    Task<Result<IReadOnlyList<MyCourseResponse>>> ListMineAsync(CancellationToken ct);
}
