using CareerPlatform.Api.Features.Support.Dto;
using CareerPlatform.Api.Features.Support.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CareerPlatform.Api.Features.Support.Controller;

/// <summary>Student-facing support: raise tickets, view mine, post replies.</summary>
[ApiController]
[Route("api/v1/support")]  // canonical
[Produces("application/json")]
[Authorize]
public sealed class SupportController : ControllerBase
{
    private readonly ISupportService _service;
    public SupportController(ISupportService service) => _service = service;

    [HttpGet("tickets/me")]
    public async Task<ActionResult<IReadOnlyList<SupportTicketResponse>>> ListMine(CancellationToken ct)
        => (await _service.ListMineAsync(ct)).ToActionResult();

    [HttpGet("tickets/{id:int}")]
    public async Task<ActionResult<SupportTicketResponse>> Get(int id, CancellationToken ct)
        => (await _service.GetAsync(id, allowAdmin: false, ct)).ToActionResult();

    [HttpPost("tickets")]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult<SupportTicketResponse>> Create(
        [FromBody] CreateTicketRequest body, CancellationToken ct)
        => (await _service.CreateAsync(body, ct)).ToActionResult();

    [HttpPost("tickets/{id:int}/messages")]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult<SupportTicketResponse>> PostMessage(
        int id, [FromBody] PostTicketMessageRequest body, CancellationToken ct)
        => (await _service.PostMessageAsync(id, body.Body, allowAdmin: false, ct)).ToActionResult();
}

/// <summary>Admin triage: full queue, status transitions, admin replies.</summary>
[ApiController]
[Route("api/v1/admin/support")]  // canonical
[Produces("application/json")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class AdminSupportController : ControllerBase
{
    private readonly ISupportService _service;
    public AdminSupportController(ISupportService service) => _service = service;

    [HttpGet("tickets")]
    public async Task<ActionResult<IReadOnlyList<SupportTicketResponse>>> List(
        [FromQuery] string? status, CancellationToken ct)
        => (await _service.ListAdminAsync(status, ct)).ToActionResult();

    [HttpGet("tickets/{id:int}")]
    public async Task<ActionResult<SupportTicketResponse>> Get(int id, CancellationToken ct)
        => (await _service.GetAsync(id, allowAdmin: true, ct)).ToActionResult();

    [HttpPost("tickets/{id:int}/messages")]
    public async Task<ActionResult<SupportTicketResponse>> PostMessage(
        int id, [FromBody] PostTicketMessageRequest body, CancellationToken ct)
        => (await _service.PostMessageAsync(id, body.Body, allowAdmin: true, ct)).ToActionResult();

    [HttpPut("tickets/{id:int}/status")]
    public async Task<ActionResult<SupportTicketResponse>> UpdateStatus(
        int id, [FromBody] UpdateTicketStatusRequest body, CancellationToken ct)
        => (await _service.UpdateStatusAsync(id, body, ct)).ToActionResult();
}
