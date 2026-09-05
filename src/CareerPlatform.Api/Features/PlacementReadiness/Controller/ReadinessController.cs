using CareerPlatform.Api.Features.PlacementReadiness.Dto;
using CareerPlatform.Api.Features.PlacementReadiness.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareerPlatform.Api.Features.PlacementReadiness.Controller;

/// <summary>
/// Placement readiness for the signed-in student.
///
/// Read-only by design: the score is derived from the student's own learning, assessment, interview,
/// skill and resume records on every request, so there is no endpoint that could set it to a value the
/// records do not support.
/// </summary>
[ApiController]
[Route("api/v1/placement-readiness")]
[Produces("application/json")]
[Authorize]
public sealed class PlacementReadinessController : ControllerBase
{
    private readonly IReadinessService _service;
    public PlacementReadinessController(IReadinessService service) => _service = service;

    /// <summary>
    /// GET /me — the caller's readiness score with its per-dimension breakdown. Components the student
    /// has no data for report a <c>null</c> score rather than a zero, and <c>coverage</c> says how much
    /// of the model was actually measurable.
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<ReadinessResponse>> Mine(CancellationToken ct)
        => (await _service.GetMyReadinessAsync(ct)).ToActionResult();
}
