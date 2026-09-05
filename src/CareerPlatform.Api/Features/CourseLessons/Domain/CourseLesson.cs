using System.ComponentModel.DataAnnotations;
using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.CourseLessons.Domain;

/// <summary>
/// An ordered lesson within a course. Content is either an external URL (video) or inline
/// markdown (article) — <see cref="LessonType"/> tells the client which to render.
///
/// Per-student completion is NOT stored here. It lives in <c>LearningProgress</c> keyed on
/// <c>(UserId, ResourceType="Lesson", ResourceId=lessonId)</c>, so the progress mechanism stays
/// polymorphic and this table stays purely about content.
/// </summary>
public sealed class CourseLesson : AuditableEntity<int>
{
    public int CourseId { get; set; }

    [Required, MaxLength(200)] public string Title { get; set; } = string.Empty;

    /// <summary><c>video</c> / <c>article</c> / <c>quiz</c>.</summary>
    [Required, MaxLength(16)] public string LessonType { get; set; } = "video";

    public int? DurationSeconds { get; set; }

    /// <summary>External media URL for <c>video</c> lessons.</summary>
    [MaxLength(1000)] public string? ContentUrl { get; set; }

    /// <summary>Inline markdown for <c>article</c> lessons.</summary>
    [MaxLength(8000)] public string? ContentMarkdown { get; set; }

    public int OrderIndex { get; set; }

    public bool IsPublished { get; set; } = true;
}
