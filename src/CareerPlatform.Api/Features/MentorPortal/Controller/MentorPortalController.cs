using CareerPlatform.Api.Features.MentorPortal.Dto;
using CareerPlatform.Api.Features.MentorPortal.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CareerPlatform.Api.Features.MentorPortal.Controller;

/// <summary>Mentor-self surface: dashboard, profile, sessions, students, slots, reviews.</summary>
[ApiController]
[Route("api/v1/mentor")]  // canonical
[Produces("application/json")]
[Authorize(Roles = "Mentor")]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class MentorPortalController : ControllerBase
{
    private readonly IMentorPortalService _service;
    public MentorPortalController(IMentorPortalService service) => _service = service;

    [HttpGet("profile")]
    public async Task<ActionResult<MentorProfileResponse>> GetProfile(CancellationToken ct)
        => (await _service.GetProfileAsync(ct)).ToActionResult();

    [HttpPut("profile")]
    public async Task<ActionResult<MentorProfileResponse>> UpdateProfile(
        [FromBody] UpdateMentorProfileRequest body, CancellationToken ct)
        => (await _service.UpdateProfileAsync(body, ct)).ToActionResult();

    [HttpGet("overview")]
    public async Task<ActionResult<MentorOverviewResponse>> Overview(CancellationToken ct)
        => (await _service.GetOverviewAsync(ct)).ToActionResult();

    [HttpGet("sessions")]
    public async Task<ActionResult<IReadOnlyList<MentorSessionResponse>>> Sessions(CancellationToken ct)
        => (await _service.ListSessionsAsync(ct)).ToActionResult();

    /// <summary>GET /sessions/{id} — a single booking of the caller's, for the session room.</summary>
    [HttpGet("sessions/{bookingId:int}")]
    public async Task<ActionResult<MentorSessionResponse>> Session(int bookingId, CancellationToken ct)
        => (await _service.GetSessionAsync(bookingId, ct)).ToActionResult();

    /// <summary>POST /sessions/{id}/complete — closes out a session the mentor has delivered.</summary>
    [HttpPost("sessions/{bookingId:int}/complete")]
    public async Task<ActionResult<MentorSessionResponse>> CompleteSession(
        int bookingId, CancellationToken ct)
        => (await _service.CompleteSessionAsync(bookingId, ct)).ToActionResult();

    [HttpGet("students")]
    public async Task<ActionResult<IReadOnlyList<MentorMenteeResponse>>> Students(CancellationToken ct)
        => (await _service.ListStudentsAsync(ct)).ToActionResult();

    [HttpGet("students/{studentUserId}")]
    public async Task<ActionResult<MentorMenteeDetailResponse>> Student(string studentUserId, CancellationToken ct)
        => (await _service.GetStudentAsync(studentUserId, ct)).ToActionResult();

    [HttpGet("slots")]
    public async Task<ActionResult<IReadOnlyList<MentorSlotItemResponse>>> Slots(CancellationToken ct)
        => (await _service.ListSlotsAsync(ct)).ToActionResult();

    [HttpPost("slots")]
    public async Task<ActionResult<MentorSlotItemResponse>> CreateSlot(
        [FromBody] CreateMentorSlotRequest body, CancellationToken ct)
        => (await _service.CreateSlotAsync(body, ct)).ToActionResult();

    [HttpDelete("slots/{id:int}")]
    public async Task<ActionResult> DeleteSlot(int id, CancellationToken ct)
        => (await _service.DeleteSlotAsync(id, ct)).ToActionResult();

    [HttpGet("reviews")]
    public async Task<ActionResult<IReadOnlyList<MentorReviewResponse>>> Reviews(CancellationToken ct)
        => (await _service.ListReviewsAsync(ct)).ToActionResult();
}
