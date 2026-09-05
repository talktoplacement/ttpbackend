using CareerPlatform.Api.Features.MentorshipPlans.Domain;

namespace CareerPlatform.Api.Features.MentorshipPlans.Dto;

public sealed record MentorshipPlanResponse(
    int Id, string Slug, string Title, string Description,
    int DurationMinutes, decimal Price, decimal CommissionPercent, bool IsPublished)
{
    public static MentorshipPlanResponse From(MentorshipPlan p)
    {
        ArgumentNullException.ThrowIfNull(p);
        return new MentorshipPlanResponse(
            p.Id, p.Slug, p.Title, p.Description,
            p.DurationMinutes, p.Price, p.CommissionPercent, p.IsPublished);
    }
}

/// <summary>Body for <c>POST /api/v1/admin/mentorship-plans</c>.</summary>
public sealed record CreateMentorshipPlanRequest(
    string Slug, string Title, string? Description,
    int DurationMinutes, decimal Price, decimal CommissionPercent, bool IsPublished = true);

/// <summary>Body for <c>PUT /api/v1/admin/mentorship-plans/{id}</c>. Every field optional.</summary>
public sealed record UpdateMentorshipPlanRequest(
    string? Slug, string? Title, string? Description,
    int? DurationMinutes, decimal? Price, decimal? CommissionPercent, bool? IsPublished);
