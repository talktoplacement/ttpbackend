using CareerPlatform.Api.Features.Reviews.Domain;
using CareerPlatform.Api.Features.Reviews.Dto;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.Reviews.Service;

internal sealed class ReviewService : IReviewService
{
    private const string StatusPending = "pending";
    private const string StatusApproved = "approved";
    private const string StatusRejected = "rejected";

    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ReviewService(AppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<ReviewResponse>>> ListPublicForCourseAsync(
        int courseId, CancellationToken ct)
    {
        var rows = await _db.CourseReviews.AsNoTracking()
            .Where(r => r.CourseId == courseId && r.Status == StatusApproved)
            .OrderByDescending(r => r.ModeratedAtUtc ?? r.CreatedAtUtc)
            .Take(PaginationRequest.MaxPageSize)
            .ToListAsync(ct);
        IReadOnlyList<ReviewResponse> items = rows.Select(ReviewResponse.From).ToList();
        return Result.Success(items);
    }

    public async Task<Result<ReviewResponse>> CreateAsync(CreateReviewRequest r, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure<ReviewResponse>(Error.Unauthorized(
                "Review.Unauthorized", "An authenticated user is required."));
        }

        var exists = await _db.CourseReviews
            .AnyAsync(x => x.UserId == userId && x.CourseId == r.CourseId, ct);
        if (exists)
        {
            return Result.Failure<ReviewResponse>(Error.Conflict(
                "Review.AlreadyExists", "You have already reviewed this course."));
        }

        var review = new CourseReview
        {
            UserId = userId,
            CourseId = r.CourseId,
            Rating = r.Rating,
            Comment = r.Comment.Trim(),
            Status = StatusPending,
        };
        _db.CourseReviews.Add(review);
        await _db.SaveChangesAsync(ct);
        return Result.Success(ReviewResponse.From(review));
    }

    public async Task<Result<IReadOnlyList<ReviewResponse>>> ListForAdminAsync(
        string? status, CancellationToken ct)
    {
        var q = _db.CourseReviews.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status))
        {
            var s = status.Trim().ToLowerInvariant();
            q = q.Where(r => r.Status == s);
        }
        var rows = await q
            .OrderByDescending(r => r.CreatedAtUtc)
            .Take(PaginationRequest.MaxPageSize)
            .ToListAsync(ct);
        IReadOnlyList<ReviewResponse> items = rows.Select(ReviewResponse.From).ToList();
        return Result.Success(items);
    }

    public async Task<Result<ReviewResponse>> ModerateAsync(
        int id, ModerateReviewRequest r, CancellationToken ct)
    {
        var moderatorId = _currentUser.UserId;
        if (string.IsNullOrEmpty(moderatorId))
        {
            return Result.Failure<ReviewResponse>(Error.Unauthorized(
                "Review.Unauthorized", "An authenticated admin is required."));
        }
        var review = await _db.CourseReviews.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (review is null)
        {
            return Result.Failure<ReviewResponse>(Error.NotFound(
                "Review.NotFound", $"Review {id} was not found."));
        }
        var action = r.Action.Trim().ToLowerInvariant();
        review.Status = action == "approve" ? StatusApproved : StatusRejected;
        review.ModeratedBy = moderatorId;
        review.ModeratedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result.Success(ReviewResponse.From(review));
    }
}
