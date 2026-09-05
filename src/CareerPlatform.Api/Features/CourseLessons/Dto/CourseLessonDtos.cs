using CareerPlatform.Api.Features.CourseLessons.Domain;

namespace CareerPlatform.Api.Features.CourseLessons.Dto;

/// <summary>Lesson content without any per-user state.</summary>
public sealed record CourseLessonResponse(
    int Id, int CourseId, string Title, string LessonType,
    int? DurationSeconds, string? ContentUrl, string? ContentMarkdown,
    int OrderIndex, bool IsPublished)
{
    public static CourseLessonResponse From(CourseLesson l)
    {
        ArgumentNullException.ThrowIfNull(l);
        return new CourseLessonResponse(
            l.Id, l.CourseId, l.Title, l.LessonType,
            l.DurationSeconds, l.ContentUrl, l.ContentMarkdown,
            l.OrderIndex, l.IsPublished);
    }
}

/// <summary>
/// Lesson + the caller's progress on it. `PercentComplete` and `Status` come from the
/// LearningProgress row for (UserId, "Lesson", lessonId); absent rows report 0 / not-started.
/// </summary>
public sealed record CourseLessonWithProgressResponse(
    int Id, int CourseId, string Title, string LessonType,
    int? DurationSeconds, string? ContentUrl, string? ContentMarkdown,
    int OrderIndex,
    int PercentComplete, string Status, string? LastAccessedAt);

/// <summary>Course-level rollup returned alongside the lesson list.</summary>
public sealed record CourseProgressSummary(
    int TotalLessons,
    int CompletedLessons,
    int PercentComplete);

public sealed record CourseLessonsWithProgressResponse(
    IReadOnlyList<CourseLessonWithProgressResponse> Lessons,
    CourseProgressSummary Summary);

public sealed record CreateCourseLessonRequest(
    string Title, string LessonType,
    int? DurationSeconds, string? ContentUrl, string? ContentMarkdown,
    int OrderIndex = 0, bool IsPublished = true);

public sealed record UpdateCourseLessonRequest(
    string Title, string LessonType,
    int? DurationSeconds, string? ContentUrl, string? ContentMarkdown,
    int OrderIndex, bool IsPublished);

/// <summary>Reorder payload: lesson ids in their new display order.</summary>
public sealed record ReorderCourseLessonsRequest(IReadOnlyList<int> OrderedIds);
