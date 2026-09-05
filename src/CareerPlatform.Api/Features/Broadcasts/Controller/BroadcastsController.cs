using CareerPlatform.Api.Features.Broadcasts.Dto;
using CareerPlatform.Api.Features.Broadcasts.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CareerPlatform.Api.Features.Broadcasts.Controller;

[ApiController]
[Route("api/v1/admin/broadcasts")]      // canonical
[Produces("application/json")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class BroadcastsController : ControllerBase
{
    private readonly IBroadcastService _service;
    public BroadcastsController(IBroadcastService service) => _service = service;

    /// <summary>GET — broadcast history, newest-first; optional <c>?type=Notification|Promotion</c>.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BroadcastResponse>>> List(
        [FromQuery] string? type, CancellationToken ct)
    {
        var result = await _service.ListAsync(type, ct);
        return result.ToActionResult();
    }

    /// <summary>GET recipients — count how many users the target-plan filter would reach.</summary>
    [HttpGet("recipients")]
    public async Task<ActionResult<RecipientCountResult>> RecipientCount(
        [FromQuery] string? plan, CancellationToken ct)
    {
        var result = await _service.GetRecipientCountAsync(plan, ct);
        return result.ToActionResult();
    }

    /// <summary>
    /// GET audience-targets — the selectable target-plan list with live recipient counts, so the
    /// admin UI renders real plans instead of a hardcoded dropdown.
    /// </summary>
    [HttpGet("audience-targets")]
    public async Task<ActionResult<IReadOnlyList<BroadcastAudienceTarget>>> AudienceTargets(
        CancellationToken ct)
    {
        var result = await _service.ListAudienceTargetsAsync(ct);
        return result.ToActionResult();
    }

    /// <summary>POST — persist a broadcast + fan out one Notification per targeted user.</summary>
    [HttpPost]
    public async Task<ActionResult<SendBroadcastResult>> Send(
        [FromBody] SendBroadcastRequest body, CancellationToken ct)
    {
        var result = await _service.SendAsync(body, ct);
        return result.ToActionResult();
    }
}

/// <summary>
/// Student-facing read surface for broadcasts. Separate from the admin controller so the admin
/// route prefix keeps its blanket <c>Admin</c> role requirement.
/// </summary>
[ApiController]
[Route("api/v1/broadcasts")]
[Produces("application/json")]
[Authorize]
public sealed class StudentBroadcastsController : ControllerBase
{
    private readonly IBroadcastService _service;
    public StudentBroadcastsController(IBroadcastService service) => _service = service;

    /// <summary>
    /// GET today — today's notification broadcasts for the signed-in student's own plan. The plan
    /// is resolved server-side from the caller's identity, never from a query parameter.
    /// </summary>
    [HttpGet("today")]
    public async Task<ActionResult<IReadOnlyList<BroadcastResponse>>> Today(CancellationToken ct)
    {
        var result = await _service.ListTodayForCurrentStudentAsync(ct);
        return result.ToActionResult();
    }
}
