using CareerPlatform.Api.Features.PlacementCompanies.Dto;
using CareerPlatform.Api.Features.PlacementCompanies.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CareerPlatform.Api.Features.PlacementCompanies.Controller;

/// <summary>Public placement-companies catalog.</summary>
[ApiController]
[Route("api/v1/placement-companies")]  // canonical
[Produces("application/json")]
public sealed class PlacementCompaniesController : ControllerBase
{
    private readonly IPlacementCompanyService _service;
    public PlacementCompaniesController(IPlacementCompanyService service) => _service = service;

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<PlacementCompanyResponse>>> List(
        [FromQuery] string? tier, CancellationToken ct)
    {
        var result = await _service.ListAsync(tier, publishedOnly: true, ct);
        return result.ToActionResult();
    }

    [HttpGet("{slug}")]
    [AllowAnonymous]
    public async Task<ActionResult<PlacementCompanyResponse>> Get(string slug, CancellationToken ct)
    {
        var result = await _service.GetAsync(slug, ct);
        return result.ToActionResult();
    }
}

/// <summary>Admin CRUD.</summary>
[ApiController]
[Route("api/v1/admin/placement-companies")]  // canonical
[Produces("application/json")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class AdminPlacementCompaniesController : ControllerBase
{
    private readonly IPlacementCompanyService _service;
    public AdminPlacementCompaniesController(IPlacementCompanyService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PlacementCompanyResponse>>> List(
        [FromQuery] string? tier, CancellationToken ct)
    {
        var result = await _service.ListAsync(tier, publishedOnly: false, ct);
        return result.ToActionResult();
    }

    /// <summary>GET /{id} — single company (any publish state) for the edit form.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PlacementCompanyResponse>> GetById(int id, CancellationToken ct)
        => (await _service.GetByIdAsync(id, ct)).ToActionResult();

    [HttpPost]
    public async Task<ActionResult<PlacementCompanyResponse>> Create(
        [FromBody] CreatePlacementCompanyRequest body, CancellationToken ct)
    {
        var result = await _service.CreateAsync(body, ct);
        return result.ToActionResult();
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PlacementCompanyResponse>> Update(
        int id, [FromBody] UpdatePlacementCompanyRequest body, CancellationToken ct)
    {
        var result = await _service.UpdateAsync(id, body, ct);
        return result.ToActionResult();
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _service.DeleteAsync(id, ct);
        return result.ToActionResult();
    }
}
