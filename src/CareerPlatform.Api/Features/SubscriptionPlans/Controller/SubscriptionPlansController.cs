using CareerPlatform.Api.Features.SubscriptionPlans.Dto;
using CareerPlatform.Api.Features.SubscriptionPlans.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CareerPlatform.Api.Features.SubscriptionPlans.Controller;

[ApiController]
[Route("api/v1/subscription-plans")]  // canonical
[Produces("application/json")]
public sealed class SubscriptionPlansController : ControllerBase
{
    private readonly ISubscriptionPlanService _service;
    public SubscriptionPlansController(ISubscriptionPlanService service) => _service = service;

    [HttpGet("catalog")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<CatalogPlanResponse>>> Catalog(CancellationToken ct)
        => (await _service.ListActiveAsync(ct)).ToActionResult();

    [HttpGet("entitlement")]
    [Authorize]
    public async Task<ActionResult<EntitlementResponse>> Entitlement(CancellationToken ct)
        => (await _service.GetEntitlementAsync(ct)).ToActionResult();

    // ---- Admin CRUD ----

    [HttpGet]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult<PaginatedResult<PlanResponse>>> List(
        [FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken ct)
        => (await _service.ListAsync(page, pageSize, ct)).ToActionResult();

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult<PlanResponse>> Get(int id, CancellationToken ct)
        => (await _service.GetAsync(id, ct)).ToActionResult();

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult<PlanResponse>> Create(
        [FromBody] CreatePlanRequest body, CancellationToken ct)
        => (await _service.CreateAsync(body, ct)).ToActionResult();

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult<PlanResponse>> Update(
        int id, [FromBody] UpdatePlanRequest body, CancellationToken ct)
        => (await _service.UpdateAsync(id, body, ct)).ToActionResult();

    [HttpPut("{id:int}/active")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult<PlanResponse>> SetActive(
        int id, [FromBody] SetPlanActiveRequest body, CancellationToken ct)
        => (await _service.SetActiveAsync(id, body.IsActive, ct)).ToActionResult();

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult> Delete(int id, CancellationToken ct)
        => (await _service.DeleteAsync(id, ct)).ToActionResult();
}
