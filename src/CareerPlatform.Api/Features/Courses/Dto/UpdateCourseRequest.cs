namespace CareerPlatform.Api.Features.Courses.Dto;

/// <summary>
/// Inbound request body for <c>PUT /api/v1/courses/{id}</c>. The <c>Id</c> is carried on the
/// URL. The new price applies only to future orders/enrollments — frozen <c>Order.Amount</c> and
/// <c>Transaction.Amount</c> snapshots on existing rows are never mutated.
/// </summary>
public sealed record UpdateCourseRequest(
    string Slug,
    string Title,
    string? Description,
    decimal Price,
    string? Currency,
    string? MediaUrl,
    bool IsPublished);
