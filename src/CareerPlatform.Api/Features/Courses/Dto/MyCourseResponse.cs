namespace CareerPlatform.Api.Features.Courses.Dto;

/// <summary>
/// Why a student has access to a course. Sent to the client so the UI can explain the grant
/// ("included in your plan" vs "purchased") instead of implying every course was bought outright.
/// </summary>
public static class CourseAccessSource
{
    /// <summary>Granted by an explicit per-course enrollment (a course purchase).</summary>
    public const string Purchase = "purchase";

    /// <summary>Granted by an active paid subscription that includes the course catalog.</summary>
    public const string Subscription = "subscription";

    /// <summary>
    /// No current grant, but the student has progress on the course — so it stays visible and
    /// resumable rather than vanishing from their library when a subscription lapses.
    /// </summary>
    public const string PreviousActivity = "previous-activity";
}

/// <summary>
/// One entry in a student's course library: the course, why they have it, and how far along they are.
///
/// Progress fields are nullable because access and progress are independent — a student can be
/// entitled to a course they have never opened (all nulls, 0%), which is exactly the case the old
/// progress-only listing could not represent.
/// </summary>
public sealed record MyCourseResponse(
    int Id,
    string Slug,
    string Title,
    string Description,
    string? MediaUrl,
    /// <summary>One of <see cref="CourseAccessSource"/>.</summary>
    string AccessSource,
    /// <summary>0–100. Zero when the student has not started.</summary>
    int PercentComplete,
    /// <summary><c>not-started</c> | <c>in-progress</c> | <c>completed</c>.</summary>
    string Status,
    string? LastAccessedAt,
    string? CompletedAt);
