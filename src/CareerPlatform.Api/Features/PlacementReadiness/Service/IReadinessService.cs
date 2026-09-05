using CareerPlatform.Api.Features.PlacementReadiness.Dto;

namespace CareerPlatform.Api.Features.PlacementReadiness.Service;

/// <summary>
/// Placement readiness for the authenticated student.
///
/// Computed on read rather than stored: every input (learning progress, assessment attempts, mock
/// interview self-assessments, declared skills, resume) already lives in its own table, so a cached
/// score would only add a way for the number to be stale. There is deliberately no setter — nothing
/// can write a readiness figure that the underlying records do not support.
/// </summary>
public interface IReadinessService
{
    Task<Result<ReadinessResponse>> GetMyReadinessAsync(CancellationToken ct);
}
