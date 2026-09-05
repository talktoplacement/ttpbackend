using CareerPlatform.Api.Features.PlacementRoles.Dto;
using CareerPlatform.Api.Features.PlacementRoles.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CareerPlatform.Api.Features.PlacementRoles.Controller;

/// <summary>Public placement-role catalog for the /placement/roles surface.</summary>
[ApiController]
[Route("api/v1/placement-roles")]
[Produces("application/json")]
[AllowAnonymous]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class PlacementRolesController : ControllerBase
{
    private readonly IPlacementRoleService _service;
    public PlacementRolesController(IPlacementRoleService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PlacementRoleResponse>>> List(CancellationToken ct)
        => (await _service.ListPublishedAsync(ct)).ToActionResult();

    [HttpGet("{slug}")]
    public async Task<ActionResult<PlacementRoleResponse>> Get(string slug, CancellationToken ct)
        => (await _service.GetBySlugAsync(slug, ct)).ToActionResult();
}

/// <summary>Admin CRUD for placement roles.</summary>
[ApiController]
[Route("api/v1/admin/placement-roles")]
[Produces("application/json")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class AdminPlacementRolesController : ControllerBase
{
    private readonly IPlacementRoleService _service;
    public AdminPlacementRolesController(IPlacementRoleService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PlacementRoleResponse>>> List(CancellationToken ct)
        => (await _service.ListAllAsync(ct)).ToActionResult();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PlacementRoleResponse>> Get(int id, CancellationToken ct)
        => (await _service.GetByIdAsync(id, ct)).ToActionResult();

    [HttpPost]
    public async Task<ActionResult<PlacementRoleResponse>> Create(
        [FromBody] CreatePlacementRoleRequest body, CancellationToken ct)
        => (await _service.CreateAsync(body, ct)).ToActionResult();

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PlacementRoleResponse>> Update(
        int id, [FromBody] UpdatePlacementRoleRequest body, CancellationToken ct)
        => (await _service.UpdateAsync(id, body, ct)).ToActionResult();

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id, CancellationToken ct)
        => (await _service.DeleteAsync(id, ct)).ToActionResult();
}
