using CareerPlatform.Api.Features.MentorAssignments.Dto;
using CareerPlatform.Api.Features.MentorAssignments.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CareerPlatform.Api.Features.MentorAssignments.Controller;

/// <summary>
/// Student-facing read of their own mentor pairing.
///
/// Separate from the admin controller so the admin prefix keeps its blanket <c>Admin</c> role
/// requirement — a student must be able to see who their mentor is without any admin grant.
/// </summary>
[ApiController]
[Route("api/v1/mentorship/my-mentor")]
[Produces("application/json")]
[Authorize]
public sealed class MyMentorController : ControllerBase
{
    private readonly IMentorAssignmentService _service;
    public MyMentorController(IMentorAssignmentService service) => _service = service;

    /// <summary>
    /// GET — the caller's active mentor, or a <c>null</c> body when no mentor has been assigned yet.
    /// Having no mentor is a normal state, so this is a 200 with no payload rather than a 404.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<MyMentorResponse?>> Get(CancellationToken ct)
        => (await _service.GetMyMentorAsync(ct)).ToActionResult();
}

/// <summary>Admin surface for manual mentor↔student cohort assignment.</summary>
[ApiController]
[Route("api/v1/admin/mentor-assignments")]
[Produces("application/json")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class AdminMentorAssignmentsController : ControllerBase
{
    private readonly IMentorAssignmentService _service;
    public AdminMentorAssignmentsController(IMentorAssignmentService service) => _service = service;

    /// <summary>GET ?activeOnly=true — assignment list with student + mentor names.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MentorAssignmentResponse>>> List(
        [FromQuery(Name = "activeOnly")] bool activeOnly, CancellationToken ct)
        => (await _service.ListAsync(activeOnly, ct)).ToActionResult();

    /// <summary>GET /eligible-students — students with no active mentor.</summary>
    [HttpGet("eligible-students")]
    public async Task<ActionResult<IReadOnlyList<EligibleStudentResponse>>> ListEligibleStudents(
        CancellationToken ct)
        => (await _service.ListEligibleStudentsAsync(ct)).ToActionResult();

    /// <summary>GET /mentor-pool — active mentors with their current assignment load.</summary>
    [HttpGet("mentor-pool")]
    public async Task<ActionResult<IReadOnlyList<MentorPoolEntryResponse>>> ListMentorPool(
        CancellationToken ct)
        => (await _service.ListMentorPoolAsync(ct)).ToActionResult();

    /// <summary>POST — assign a mentor. 409 when the student already has an active mentor.</summary>
    [HttpPost]
    public async Task<ActionResult<MentorAssignmentResponse>> Create(
        [FromBody] CreateMentorAssignmentRequest body, CancellationToken ct)
        => (await _service.CreateAsync(body, ct)).ToActionResult();

    [HttpPut("{id:int}")]
    public async Task<ActionResult<MentorAssignmentResponse>> Update(
        int id, [FromBody] UpdateMentorAssignmentRequest body, CancellationToken ct)
        => (await _service.UpdateAsync(id, body, ct)).ToActionResult();

    /// <summary>POST /{id}/end — soft-close the assignment, freeing the student for reassignment.</summary>
    [HttpPost("{id:int}/end")]
    public async Task<ActionResult<MentorAssignmentResponse>> End(int id, CancellationToken ct)
        => (await _service.EndAsync(id, ct)).ToActionResult();
}
