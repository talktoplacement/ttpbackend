using System.Text.Json;
using CareerPlatform.Api.Features.LearningPaths.Domain;

namespace CareerPlatform.Api.Features.LearningPaths.Dto;

public sealed record LearningPathMilestoneDto(
    string Id, string Title, string Description, int DurationWeeks, int CoursesCount);

public sealed record LearningPathResponse(
    string Id, string Slug, string Title, string Description, string TargetRole,
    int EstimatedMonths, IReadOnlyList<LearningPathMilestoneDto> Milestones,
    bool IsPopular, bool IsPublished)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public static LearningPathResponse From(LearningPath p)
    {
        ArgumentNullException.ThrowIfNull(p);
        var milestones = Array.Empty<LearningPathMilestoneDto>();
        if (!string.IsNullOrWhiteSpace(p.MilestonesJson))
        {
            try
            {
                milestones = JsonSerializer.Deserialize<LearningPathMilestoneDto[]>(p.MilestonesJson, JsonOptions)
                    ?? Array.Empty<LearningPathMilestoneDto>();
            }
            catch (JsonException) { /* malformed JSON → empty list */ }
        }
        return new LearningPathResponse(
            p.Id.ToString(), p.Slug, p.Title, p.Description, p.TargetRole,
            p.EstimatedMonths, milestones, p.IsPopular, p.IsPublished);
    }
}

/// <summary>Body for <c>POST /api/v1/admin/learning-paths</c>.</summary>
public sealed record CreateLearningPathRequest(
    string Slug, string Title, string? Description, string TargetRole,
    int EstimatedMonths, IReadOnlyList<LearningPathMilestoneDto>? Milestones,
    bool IsPopular, bool IsPublished);

/// <summary>Body for <c>PUT /api/v1/admin/learning-paths/{id}</c>. All fields optional.</summary>
public sealed record UpdateLearningPathRequest(
    string? Slug, string? Title, string? Description, string? TargetRole,
    int? EstimatedMonths, IReadOnlyList<LearningPathMilestoneDto>? Milestones,
    bool? IsPopular, bool? IsPublished);
