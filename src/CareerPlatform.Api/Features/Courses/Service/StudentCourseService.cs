using CareerPlatform.Api.Features.Courses.Domain;
using CareerPlatform.Api.Features.Courses.Dto;
using CareerPlatform.Api.Features.Learning.Domain;
using CareerPlatform.Api.Features.Orders.Domain;
using CareerPlatform.Api.Features.SubscriptionPlans.Lifecycle;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.Courses.Service;

internal sealed class StudentCourseService : IStudentCourseService
{
    /// <summary>
    /// <c>LearningProgress.ResourceType</c> discriminator for courses. Matches the value the
    /// Learning feature's allow-list accepts.
    /// </summary>
    private const string CourseResourceType = "Course";

    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public StudentCourseService(AppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<MyCourseResponse>>> ListMineAsync(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure<IReadOnlyList<MyCourseResponse>>(Error.Unauthorized(
                "Course.Unauthorized", "An authenticated user is required."));
        }

        var now = DateTime.UtcNow;

        // 1) Explicit per-course grants. This is the forward-looking path: nothing writes Enrollment
        //    rows yet (there is no course checkout), but reading them here means the day one is added
        //    the library is already correct.
        var enrolledCourseIds = await _db.Enrollments.AsNoTracking()
            .Where(e => e.UserId == userId && e.ProductType == OrderProductType.Course)
            .Select(e => e.ProductId)
            .ToListAsync(ct);

        // 2) Subscription entitlement. A paid plan includes the published catalog — the data model has
        //    no plan→course mapping, so "all published courses" is the only rule it can express.
        //    Derived from the Subscriptions table (the authoritative source) rather than the
        //    denormalised UserProfile.PlanName cache.
        var effectivePlan = await EntitlementDeriver.DeriveEffectivePlanAsync(_db, userId, now, ct);
        var hasPaidPlan = EntitlementDeriver.IsProPlan(effectivePlan);

        // 3) Progress, which doubles as a third access source: a course the student has already worked
        //    on stays in their library even if the subscription that granted it has lapsed. Losing
        //    access should not erase history.
        var progressByCourseId = await _db.LearningProgress.AsNoTracking()
            .Where(p => p.UserId == userId && p.ResourceType == CourseResourceType)
            .ToDictionaryAsync(p => p.ResourceId, ct);

        // One query for every course that could appear, instead of per-course lookups.
        var candidateQuery = _db.Courses.AsNoTracking().Where(c =>
            hasPaidPlan
                ? c.IsPublished
                  || enrolledCourseIds.Contains(c.Id)
                  || progressByCourseId.Keys.Contains(c.Id)
                : enrolledCourseIds.Contains(c.Id) || progressByCourseId.Keys.Contains(c.Id));

        var courses = await candidateQuery
            .OrderBy(c => c.Title)
            .Take(PaginationRequest.MaxPageSize)
            .ToListAsync(ct);

        var items = courses
            .Select(course =>
            {
                progressByCourseId.TryGetValue(course.Id, out var progress);
                return Project(course, progress, enrolledCourseIds.Contains(course.Id), hasPaidPlan);
            })
            // Most recently touched first, then never-started courses alphabetically. Puts "resume
            // what you were doing" at the top, which is what the page is for.
            .OrderByDescending(c => c.LastAccessedAt ?? string.Empty)
            .ThenBy(c => c.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Result.Success<IReadOnlyList<MyCourseResponse>>(items);
    }

    /// <summary>
    /// Builds the library entry. Access source is reported most-specific-first: an explicit purchase
    /// outranks a subscription, which outranks lingering activity.
    /// </summary>
    private static MyCourseResponse Project(
        Course course, LearningProgress? progress, bool isEnrolled, bool hasPaidPlan)
    {
        var accessSource =
            isEnrolled ? CourseAccessSource.Purchase
            : hasPaidPlan && course.IsPublished ? CourseAccessSource.Subscription
            : CourseAccessSource.PreviousActivity;

        return new MyCourseResponse(
            course.Id,
            course.Slug,
            course.Title,
            course.Description,
            course.MediaUrl,
            accessSource,
            progress?.PercentComplete ?? 0,
            progress?.Status ?? "not-started",
            progress?.LastAccessedAtUtc.ToString("O"),
            progress?.CompletedAtUtc?.ToString("O"));
    }
}
