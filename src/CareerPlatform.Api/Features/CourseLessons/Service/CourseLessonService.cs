using CareerPlatform.Api.Features.CourseLessons.Domain;
using CareerPlatform.Api.Features.CourseLessons.Dto;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.CourseLessons.Service;

internal sealed class CourseLessonService : ICourseLessonService
{
    /// <summary>
    /// ResourceType discriminator used when reading/writing LearningProgress rows for lessons.
    /// Must match what the client sends to PUT /api/v1/learning/progress/{resourceType}/{id}.
    /// </summary>
    private const string LessonResourceType = "Lesson";
    private const string StatusCompleted = "completed";
    private const string StatusNotStarted = "not-started";

    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public CourseLessonService(AppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<CourseLessonsWithProgressResponse>> ListForStudentAsync(
        int courseId, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure<CourseLessonsWithProgressResponse>(Error.Unauthorized(
                "CourseLesson.Unauthorized", "An authenticated user is required."));
        }

        var courseExists = await _db.Courses.AsNoTracking().AnyAsync(c => c.Id == courseId, ct);
        if (!courseExists)
        {
            return Result.Failure<CourseLessonsWithProgressResponse>(Error.NotFound(
                "Course.NotFound", $"Course {courseId} was not found."));
        }

        var lessons = await _db.CourseLessons.AsNoTracking()
            .Where(l => l.CourseId == courseId && l.IsPublished)
            .OrderBy(l => l.OrderIndex).ThenBy(l => l.Id)
            .ToListAsync(ct);

        if (lessons.Count == 0)
        {
            return Result.Success(new CourseLessonsWithProgressResponse(
                Array.Empty<CourseLessonWithProgressResponse>(),
                new CourseProgressSummary(0, 0, 0)));
        }

        // Single round-trip for the caller's progress across every lesson in this course.
        var lessonIds = lessons.Select(l => l.Id).ToList();
        var progressByLessonId = await _db.LearningProgress.AsNoTracking()
            .Where(p => p.UserId == userId
                        && p.ResourceType == LessonResourceType
                        && lessonIds.Contains(p.ResourceId))
            .ToDictionaryAsync(p => p.ResourceId, ct);

        var rows = lessons.Select(l =>
        {
            progressByLessonId.TryGetValue(l.Id, out var p);
            return new CourseLessonWithProgressResponse(
                l.Id, l.CourseId, l.Title, l.LessonType,
                l.DurationSeconds, l.ContentUrl, l.ContentMarkdown,
                l.OrderIndex,
                p?.PercentComplete ?? 0,
                p?.Status ?? StatusNotStarted,
                p?.LastAccessedAtUtc.ToString("O"));
        }).ToList();

        var completed = rows.Count(r => r.Status == StatusCompleted);
        var summary = new CourseProgressSummary(
            TotalLessons: rows.Count,
            CompletedLessons: completed,
            PercentComplete: rows.Count == 0
                ? 0
                : (int)Math.Round(rows.Average(r => (double)r.PercentComplete)));

        return Result.Success(new CourseLessonsWithProgressResponse(rows, summary));
    }

    public async Task<Result<IReadOnlyList<CourseLessonResponse>>> ListForAdminAsync(
        int courseId, CancellationToken ct)
    {
        var rows = await _db.CourseLessons.AsNoTracking()
            .Where(l => l.CourseId == courseId)
            .OrderBy(l => l.OrderIndex).ThenBy(l => l.Id)
            .Take(PaginationRequest.MaxPageSize).ToListAsync(ct);
        return Result.Success((IReadOnlyList<CourseLessonResponse>)rows.Select(CourseLessonResponse.From).ToList());
    }

    public async Task<Result<CourseLessonResponse>> CreateAsync(
        int courseId, CreateCourseLessonRequest r, CancellationToken ct)
    {
        var courseExists = await _db.Courses.AsNoTracking().AnyAsync(c => c.Id == courseId, ct);
        if (!courseExists)
        {
            return Result.Failure<CourseLessonResponse>(Error.Validation(
                "Course.NotFound", $"Course {courseId} does not exist."));
        }

        // When the caller passes OrderIndex 0 (the default), append to the end rather than
        // colliding with an existing first lesson.
        var orderIndex = r.OrderIndex;
        if (orderIndex == 0)
        {
            var maxOrder = await _db.CourseLessons
                .Where(l => l.CourseId == courseId)
                .MaxAsync(l => (int?)l.OrderIndex, ct) ?? -1;
            orderIndex = maxOrder + 1;
        }

        var lesson = new CourseLesson
        {
            CourseId = courseId,
            Title = r.Title.Trim(),
            LessonType = r.LessonType.Trim().ToLowerInvariant(),
            DurationSeconds = r.DurationSeconds,
            ContentUrl = r.ContentUrl?.Trim(),
            ContentMarkdown = r.ContentMarkdown?.Trim(),
            OrderIndex = orderIndex,
            IsPublished = r.IsPublished,
        };
        _db.CourseLessons.Add(lesson);
        await _db.SaveChangesAsync(ct);
        return Result.Success(CourseLessonResponse.From(lesson));
    }

    public async Task<Result<CourseLessonResponse>> UpdateAsync(
        int courseId, int lessonId, UpdateCourseLessonRequest r, CancellationToken ct)
    {
        var lesson = await _db.CourseLessons
            .FirstOrDefaultAsync(l => l.Id == lessonId && l.CourseId == courseId, ct);
        if (lesson is null)
        {
            return Result.Failure<CourseLessonResponse>(Error.NotFound(
                "CourseLesson.NotFound", $"Lesson {lessonId} was not found on course {courseId}."));
        }
        lesson.Title = r.Title.Trim();
        lesson.LessonType = r.LessonType.Trim().ToLowerInvariant();
        lesson.DurationSeconds = r.DurationSeconds;
        lesson.ContentUrl = r.ContentUrl?.Trim();
        lesson.ContentMarkdown = r.ContentMarkdown?.Trim();
        lesson.OrderIndex = r.OrderIndex;
        lesson.IsPublished = r.IsPublished;
        await _db.SaveChangesAsync(ct);
        return Result.Success(CourseLessonResponse.From(lesson));
    }

    public async Task<Result> DeleteAsync(int courseId, int lessonId, CancellationToken ct)
    {
        var lesson = await _db.CourseLessons
            .FirstOrDefaultAsync(l => l.Id == lessonId && l.CourseId == courseId, ct);
        if (lesson is null)
        {
            return Result.Failure(Error.NotFound(
                "CourseLesson.NotFound", $"Lesson {lessonId} was not found on course {courseId}."));
        }

        // Clean up orphaned progress rows for this lesson so the polymorphic table doesn't
        // accumulate references to deleted content.
        var orphanedProgress = await _db.LearningProgress
            .Where(p => p.ResourceType == LessonResourceType && p.ResourceId == lessonId)
            .ToListAsync(ct);
        _db.LearningProgress.RemoveRange(orphanedProgress);

        _db.CourseLessons.Remove(lesson);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> ReorderAsync(
        int courseId, ReorderCourseLessonsRequest r, CancellationToken ct)
    {
        var lessons = await _db.CourseLessons
            .Where(l => l.CourseId == courseId)
            .ToListAsync(ct);

        var byId = lessons.ToDictionary(l => l.Id);
        // Reject the whole batch if any id doesn't belong to this course — a partial reorder
        // would leave the list in a state the caller didn't ask for.
        var unknown = r.OrderedIds.Where(id => !byId.ContainsKey(id)).ToList();
        if (unknown.Count > 0)
        {
            return Result.Failure(Error.Validation(
                "CourseLesson.UnknownIds",
                $"These lesson ids do not belong to course {courseId}: {string.Join(", ", unknown)}."));
        }

        for (var i = 0; i < r.OrderedIds.Count; i++)
        {
            byId[r.OrderedIds[i]].OrderIndex = i;
        }
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
