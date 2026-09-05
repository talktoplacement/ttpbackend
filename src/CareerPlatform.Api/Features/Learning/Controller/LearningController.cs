using CareerPlatform.Api.Features.Learning.Dto;
using CareerPlatform.Api.Features.Learning.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CareerPlatform.Api.Features.Learning.Controller;

[ApiController]
[Route("api/v1/learning")]   // canonical
[Produces("application/json")]
[Authorize]
public sealed class LearningController : ControllerBase
{
    private readonly ILearningService _service;
    public LearningController(ILearningService service) => _service = service;

    /// <summary>GET /progress/me — my progress rows, optionally filtered by <c>?resourceType=</c>.</summary>
    [HttpGet("progress/me")]
    public async Task<ActionResult<IReadOnlyList<LearningProgressResponse>>> ListMine(
        [FromQuery] string? resourceType, CancellationToken ct)
    {
        var result = await _service.ListMineAsync(resourceType, ct);
        return result.ToActionResult();
    }

    /// <summary>GET /progress/summary — rolled-up progress across all resource types.</summary>
    [HttpGet("progress/summary")]
    public async Task<ActionResult<LearningProgressSummary>> Summary(CancellationToken ct)
    {
        var result = await _service.GetSummaryAsync(ct);
        return result.ToActionResult();
    }

    /// <summary>PUT /progress/{resourceType}/{resourceId} — idempotent upsert.</summary>
    [HttpPut("progress/{resourceType}/{resourceId:int}")]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult<LearningProgressResponse>> Upsert(
        string resourceType, int resourceId,
        [FromBody] UpsertProgressRequest body, CancellationToken ct)
    {
        var result = await _service.UpsertAsync(resourceType, resourceId, body, ct);
        return result.ToActionResult();
    }
}
