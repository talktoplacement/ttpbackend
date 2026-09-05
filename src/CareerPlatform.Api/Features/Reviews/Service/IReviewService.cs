using CareerPlatform.Api.Features.Reviews.Dto;

namespace CareerPlatform.Api.Features.Reviews.Service;

public interface IReviewService
{
    /// <summary>Public: approved reviews for a course, newest first.</summary>
    Task<Result<IReadOnlyList<ReviewResponse>>> ListPublicForCourseAsync(int courseId, CancellationToken ct);

    /// <summary>Student: submit a new review for a course. Enters pending queue.</summary>
    Task<Result<ReviewResponse>> CreateAsync(CreateReviewRequest request, CancellationToken ct);

    /// <summary>Admin: list by status (default <c>pending</c>).</summary>
    Task<Result<IReadOnlyList<ReviewResponse>>> ListForAdminAsync(string? status, CancellationToken ct);

    /// <summary>Admin: approve/reject a pending review.</summary>
    Task<Result<ReviewResponse>> ModerateAsync(int id, ModerateReviewRequest request, CancellationToken ct);
}
