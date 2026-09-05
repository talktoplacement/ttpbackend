using CareerPlatform.Api.Features.Reviews.Domain;

namespace CareerPlatform.Api.Features.Reviews.Dto;

public sealed record ReviewResponse(
    int Id,
    string UserId,
    int CourseId,
    int Rating,
    string Comment,
    string Status,
    string? ModeratedBy,
    string? ModeratedAt,
    string CreatedAt)
{
    public static ReviewResponse From(CourseReview r)
    {
        ArgumentNullException.ThrowIfNull(r);
        return new ReviewResponse(
            r.Id, r.UserId, r.CourseId, r.Rating, r.Comment, r.Status,
            r.ModeratedBy, r.ModeratedAtUtc?.ToString("O"),
            r.CreatedAtUtc.ToString("O"));
    }
}

/// <summary>Student-facing review submission (POST from a course-detail page).</summary>
public sealed record CreateReviewRequest(int CourseId, int Rating, string Comment);

/// <summary>Admin moderation action. <c>action</c> must be <c>approve</c> or <c>reject</c>.</summary>
public sealed record ModerateReviewRequest(string Action);
