using CareerPlatform.Api.Features.Cms.Dto;
using CareerPlatform.Api.Features.Cms.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CareerPlatform.Api.Features.Cms.Controller;

/// <summary>Public: the landing hero/CTA configuration.</summary>
[ApiController]
[Route("api/v1/cms/homepage")]  // canonical
[Produces("application/json")]
public sealed class CmsHomepageController : ControllerBase
{
    private readonly ICmsHomepageService _service;
    public CmsHomepageController(ICmsHomepageService service) => _service = service;

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<CmsHomepageResponse>> Get(CancellationToken ct)
        => (await _service.GetAsync(ct)).ToActionResult();
}

/// <summary>Admin: read + upsert the singleton homepage configuration.</summary>
[ApiController]
[Route("api/v1/admin/cms/homepage")]  // canonical
[Produces("application/json")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class AdminCmsHomepageController : ControllerBase
{
    private readonly ICmsHomepageService _service;
    public AdminCmsHomepageController(ICmsHomepageService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<CmsHomepageResponse>> Get(CancellationToken ct)
        => (await _service.GetAsync(ct)).ToActionResult();

    [HttpPut]
    public async Task<ActionResult<CmsHomepageResponse>> Update(
        [FromBody] UpdateCmsHomepageRequest body, CancellationToken ct)
        => (await _service.UpdateAsync(body, ct)).ToActionResult();
}
