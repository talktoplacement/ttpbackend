using CareerPlatform.Api.Features.PlacementPlans.Dto;
using CareerPlatform.Api.Features.PlacementPlans.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CareerPlatform.Api.Features.PlacementPlans.Controller;

/// <summary>Public placement-plans catalog.</summary>
[ApiController]
[Route("api/v1/placement-plans")]  // canonical
[Produces("application/json")]
public sealed class PlacementPlansController : ControllerBase
{
    private readonly IPlacementPlanService _service;
    public PlacementPlansController(IPlacementPlanService service) => _service = service;

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<PlacementPlanResponse>>> List(CancellationToken ct)
        => (await _service.ListAsync(publishedOnly: true, ct)).ToActionResult();
}

/// <summary>Admin CRUD.</summary>
[ApiController]
[Route("api/v1/admin/placement-plans")]  // canonical
[Produces("application/json")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class AdminPlacementPlansController : ControllerBase
{
    private readonly IPlacementPlanService _service;
    public AdminPlacementPlansController(IPlacementPlanService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PlacementPlanResponse>>> List(CancellationToken ct)
        => (await _service.ListAsync(publishedOnly: false, ct)).ToActionResult();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PlacementPlanResponse>> GetById(int id, CancellationToken ct)
        => (await _service.GetByIdAsync(id, ct)).ToActionResult();

    [HttpPost]
    public async Task<ActionResult<PlacementPlanResponse>> Create(
        [FromBody] CreatePlacementPlanRequest body, CancellationToken ct)
        => (await _service.CreateAsync(body, ct)).ToActionResult();

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PlacementPlanResponse>> Update(
        int id, [FromBody] UpdatePlacementPlanRequest body, CancellationToken ct)
        => (await _service.UpdateAsync(id, body, ct)).ToActionResult();

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id, CancellationToken ct)
        => (await _service.DeleteAsync(id, ct)).ToActionResult();
}
