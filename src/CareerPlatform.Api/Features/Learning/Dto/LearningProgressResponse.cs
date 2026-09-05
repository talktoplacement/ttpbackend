using CareerPlatform.Api.Features.Learning.Domain;

namespace CareerPlatform.Api.Features.Learning.Dto;

public sealed record LearningProgressResponse(
    int Id,
    string ResourceType,
    int ResourceId,
    string Status,
    int PercentComplete,
    string LastAccessedAt,
    string? CompletedAt,
    string? Notes)
{
    public static LearningProgressResponse From(LearningProgress p)
    {
        ArgumentNullException.ThrowIfNull(p);
        return new LearningProgressResponse(
            p.Id, p.ResourceType, p.ResourceId, p.Status, p.PercentComplete,
            p.LastAccessedAtUtc.ToString("O"),
            p.CompletedAtUtc?.ToString("O"),
            p.Notes);
    }
}

public sealed record LearningProgressSummary(
    int TotalTrackedResources,
    int InProgressCount,
    int CompletedCount,
    int AveragePercentComplete,
    IReadOnlyList<LearningProgressByType> ByResourceType);

public sealed record LearningProgressByType(
    string ResourceType,
    int Total,
    int InProgress,
    int Completed,
    int AveragePercentComplete);

/// <summary>Body for <c>PUT /api/v1/learning/progress/{resourceType}/{resourceId}</c>.</summary>
public sealed record UpsertProgressRequest(int PercentComplete, string? Status = null, string? Notes = null);
