using CareerPlatform.Api.Features.Courses.Domain;

namespace CareerPlatform.Api.Features.Courses.Dto;

/// <summary>
/// Outward-facing projection of a <see cref="Course"/> returned by every Courses endpoint. Exposes
/// only scalar catalog fields — never the entity or its navigation graph.
/// </summary>
public sealed record CourseResponse(
    int Id,
    string Slug,
    string Title,
    string Description,
    decimal Price,
    string Currency,
    bool IsPublished,
    string? MediaUrl,
    DateTime CreatedAt)
{
    /// <summary>Maps a <see cref="Course"/> entity to its outward-facing projection.</summary>
    public static CourseResponse From(Course course)
    {
        ArgumentNullException.ThrowIfNull(course);

        // The Course entity carries no Currency column (parity with the legacy schema); all
        // course prices are transacted in INR, matching Razorpay's INR-only pipeline.
        return new CourseResponse(
            course.Id,
            course.Slug,
            course.Title,
            course.Description,
            course.Price,
            "INR",
            course.IsPublished,
            course.MediaUrl,
            course.CreatedAt);
    }
}
