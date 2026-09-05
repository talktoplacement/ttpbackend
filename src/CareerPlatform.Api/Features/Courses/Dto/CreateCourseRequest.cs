namespace CareerPlatform.Api.Features.Courses.Dto;

/// <summary>
/// Inbound request body for <c>POST /api/v1/courses</c>. Admin-settable catalog fields only —
/// <c>Id</c>, <c>CreatedAt</c>, and audit fields are excluded so they cannot be mass-assigned.
/// </summary>
public sealed record CreateCourseRequest(
    string Slug,
    string Title,
    string? Description,
    decimal Price,
    string? Currency,
    string? MediaUrl,
    bool IsPublished);
