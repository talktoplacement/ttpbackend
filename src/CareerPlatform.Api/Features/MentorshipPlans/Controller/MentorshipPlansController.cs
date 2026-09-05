using CareerPlatform.Api.Features.MentorshipPlans.Dto;
using CareerPlatform.Api.Features.MentorshipPlans.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CareerPlatform.Api.Features.MentorshipPlans.Controller;

/// <summary>Public mentorship-plans catalog.</summary>
[ApiController]
[Route("api/v1/mentorship-plans")]  // canonical
[Produces("application/json")]
public sealed class MentorshipPlansController : ControllerBase
{
    private readonly IMentorshipPlanService _service;
    public MentorshipPlansController(IMentorshipPlanService service) => _service = service;

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<MentorshipPlanResponse>>> List(CancellationToken ct)
        => (await _service.ListAsync(publishedOnly: true, ct)).ToActionResult();
}

/// <summary>Admin CRUD.</summary>
[ApiController]
[Route("api/v1/admin/mentorship-plans")]  // canonical
[Produces("application/json")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class AdminMentorshipPlansController : ControllerBase
{
    private readonly IMentorshipPlanService _service;
    public AdminMentorshipPlansController(IMentorshipPlanService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MentorshipPlanResponse>>> List(CancellationToken ct)
        => (await _service.ListAsync(publishedOnly: false, ct)).ToActionResult();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MentorshipPlanResponse>> GetById(int id, CancellationToken ct)
        => (await _service.GetByIdAsync(id, ct)).ToActionResult();

    [HttpPost]
    public async Task<ActionResult<MentorshipPlanResponse>> Create(
        [FromBody] CreateMentorshipPlanRequest body, CancellationToken ct)
        => (await _service.CreateAsync(body, ct)).ToActionResult();

    [HttpPut("{id:int}")]
    public async Task<ActionResult<MentorshipPlanResponse>> Update(
        int id, [FromBody] UpdateMentorshipPlanRequest body, CancellationToken ct)
        => (await _service.UpdateAsync(id, body, ct)).ToActionResult();

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id, CancellationToken ct)
        => (await _service.DeleteAsync(id, ct)).ToActionResult();
}
