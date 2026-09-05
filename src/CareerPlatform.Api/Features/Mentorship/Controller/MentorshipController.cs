using CareerPlatform.Api.Features.Mentorship.Dto;
using CareerPlatform.Api.Features.Mentorship.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CareerPlatform.Api.Features.Mentorship.Controller;

/// <summary>
/// Student + admin mentorship surface — mentor catalog, slot browsing, booking, and admin
/// slot/booking management. Dual-route: canonical <c>/api/v1/mentorship</c> + legacy
/// <c>/api/Mentorship</c>.
/// </summary>
[ApiController]
[Route("api/v1/mentorship")]   // canonical
[Produces("application/json")]
public sealed class MentorshipController : ControllerBase
{
    private readonly IMentorshipService _service;
    public MentorshipController(IMentorshipService service) => _service = service;

    // ── Student catalog + booking ───────────────────────────────────────────

    /// <summary>GET /mentors — active + verified mentor catalog (authenticated).</summary>
    [HttpGet("mentors")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<MentorResponse>>> ListMentors(
        [FromQuery] string? expertise, CancellationToken ct)
        => (await _service.ListMentorsAsync(expertise, activeOnly: true, ct)).ToActionResult();

    /// <summary>
    /// GET /public/mentors — anonymous marketing catalog of verified + active mentors.
    /// Returns <see cref="PublicMentorResponse"/>, which omits mentor emails, so no PII is exposed
    /// to unauthenticated callers.
    /// </summary>
    [HttpGet("public/mentors")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<PublicMentorResponse>>> ListPublicMentors(
        [FromQuery] string? expertise, CancellationToken ct)
        => (await _service.ListPublicMentorsAsync(expertise, ct)).ToActionResult();

    /// <summary>GET /mentors/{mentorId}/slots — upcoming unbooked slots for a mentor.</summary>
    [HttpGet("mentors/{mentorId:int}/slots")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<MentorSlotResponse>>> ListMentorSlots(
        int mentorId, CancellationToken ct)
        => (await _service.ListMentorSlotsAsync(mentorId, ct)).ToActionResult();

    /// <summary>POST /book — student books a specific mentor slot.</summary>
    [HttpPost("book")]
    [Authorize]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult<MentorBookingResponse>> Book(
        [FromBody] BookMentorSlotRequest body, CancellationToken ct)
        => (await _service.BookAsync(body, ct)).ToActionResult();

    /// <summary>GET /my-bookings — authenticated student's own bookings.</summary>
    /// <summary>POST /bookings/{id}/review — rate a completed 1:1 session.</summary>
    [HttpPost("bookings/{bookingId:int}/review")]
    [Authorize]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult<MentorBookingResponse>> ReviewBooking(
        int bookingId, [FromBody] SubmitMentorReviewRequest body, CancellationToken ct)
        => (await _service.SubmitReviewAsync(bookingId, body, ct)).ToActionResult();

    [HttpGet("my-bookings")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<MentorBookingResponse>>> MyBookings(CancellationToken ct)
        => (await _service.ListMyBookingsAsync(ct)).ToActionResult();

    // ── Admin slot CRUD ─────────────────────────────────────────────────────

    /// <summary>POST /admin/slots — admin bulk-creates mentor availability slots (dedup safe).</summary>
    [HttpPost("admin/slots")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult<IReadOnlyList<MentorSlotResponse>>> CreateSlots(
        [FromBody] CreateMentorSlotsRequest body, CancellationToken ct)
        => (await _service.CreateSlotsAsync(body, ct)).ToActionResult();

    /// <summary>DELETE /admin/slots/{id} — admin removes an unbooked slot.</summary>
    [HttpDelete("admin/slots/{id:int}")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult> DeleteSlot(int id, CancellationToken ct)
        => (await _service.DeleteSlotAsync(id, ct)).ToActionResult();

    // ── Admin bookings ──────────────────────────────────────────────────────

    /// <summary>GET /admin/bookings — every booking, newest-first.</summary>
    [HttpGet("admin/bookings")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult<IReadOnlyList<MentorBookingResponse>>> AdminBookings(CancellationToken ct)
        => (await _service.ListAdminBookingsAsync(ct)).ToActionResult();

    /// <summary>POST /admin/bookings/{id}/cancel — admin cancels; frees the underlying slot.</summary>
    [HttpPost("admin/bookings/{id:int}/cancel")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult> CancelBooking(int id, CancellationToken ct)
        => (await _service.CancelBookingAsync(id, ct)).ToActionResult();
}

/// <summary>
/// Admin mentor-lifecycle surface (create / update / list mentors) on
/// <c>/api/v1/admin/mentors</c>. Slot and booking administration lives on
/// <see cref="MentorshipController"/> under <c>/api/v1/mentorship/admin/*</c>, not here.
/// </summary>
[ApiController]
[Route("api/v1/admin/mentors")]           // canonical
[Produces("application/json")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class AdminMentorsController : ControllerBase
{
    private readonly IMentorshipService _service;
    public AdminMentorsController(IMentorshipService service) => _service = service;

    /// <summary>GET — every mentor (including pending/suspended) for the admin grid.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MentorResponse>>> List(
        [FromQuery] string? expertise, CancellationToken ct)
        => (await _service.ListMentorsAsync(expertise, activeOnly: false, ct)).ToActionResult();

    /// <summary>GET /{id} — a single mentor (any status) for the admin detail / edit form.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<MentorResponse>> GetById(int id, CancellationToken ct)
        => (await _service.GetMentorByIdAsync(id, ct)).ToActionResult();

    /// <summary>POST — onboard a new mentor (Pending until verified).</summary>
    [HttpPost]
    public async Task<ActionResult<MentorResponse>> Onboard(
        [FromBody] OnboardMentorRequest body, CancellationToken ct)
        => (await _service.OnboardAsync(body, ct)).ToActionResult();

    /// <summary>PUT — partial mentor update (id in body). Retained for existing callers.</summary>
    [HttpPut]
    public async Task<ActionResult<MentorResponse>> Update(
        [FromBody] UpdateMentorRequest body, CancellationToken ct)
        => (await _service.UpdateAsync(body, ct)).ToActionResult();

    /// <summary>PUT /{id} — route-addressed update; the route id wins over any body id.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<MentorResponse>> UpdateById(
        int id, [FromBody] UpdateMentorRequest body, CancellationToken ct)
        => (await _service.UpdateAsync(body with { Id = id }, ct)).ToActionResult();
}
