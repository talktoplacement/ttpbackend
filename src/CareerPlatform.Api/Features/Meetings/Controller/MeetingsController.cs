using CareerPlatform.Api.Features.Meetings.Dto;
using CareerPlatform.Api.Features.Meetings.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CareerPlatform.Api.Features.Meetings.Controller;

[ApiController]
[Route("api/v1/admin/meetings")]         // canonical
[Produces("application/json")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class MeetingsController : ControllerBase
{
    private readonly IMeetingService _service;
    public MeetingsController(IMeetingService service) => _service = service;

    /// <summary>GET — every admin-scheduled meeting, newest-first.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MeetingResponse>>> List(CancellationToken ct)
    {
        var result = await _service.ListAsync(ct);
        return result.ToActionResult();
    }

    /// <summary>POST — schedule a new meeting or cohort webinar.</summary>
    [HttpPost]
    public async Task<ActionResult<MeetingResponse>> Schedule(
        [FromBody] ScheduleMeetingRequest body, CancellationToken ct)
    {
        var result = await _service.ScheduleAsync(body, ct);
        return result.ToActionResult();
    }

    /// <summary>PUT — partial update of status / scheduledAt / meetUrl (id in body).</summary>
    [HttpPut]
    public async Task<ActionResult<MeetingResponse>> Update(
        [FromBody] UpdateMeetingRequest body, CancellationToken ct)
    {
        var result = await _service.UpdateAsync(body, ct);
        return result.ToActionResult();
    }

    /// <summary>DELETE — soft-cancel by setting status = Cancelled (id in query).</summary>
    [HttpDelete]
    public async Task<ActionResult> Cancel([FromQuery] int id, CancellationToken ct)
    {
        var result = await _service.CancelAsync(id, ct);
        return result.ToActionResult();
    }
}
