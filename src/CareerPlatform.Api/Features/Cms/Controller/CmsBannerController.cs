using CareerPlatform.Api.Features.Cms.Dto;
using CareerPlatform.Api.Features.Cms.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CareerPlatform.Api.Features.Cms.Controller;

/// <summary>Public: active header banners.</summary>
[ApiController]
[Route("api/v1/cms/banners")]  // canonical
[Produces("application/json")]
public sealed class CmsBannersController : ControllerBase
{
    private readonly ICmsBannerService _service;
    public CmsBannersController(ICmsBannerService service) => _service = service;

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<CmsBannerResponse>>> List(CancellationToken ct)
        => (await _service.ListAsync(activeOnly: true, ct)).ToActionResult();
}

/// <summary>Admin CRUD for header banners.</summary>
[ApiController]
[Route("api/v1/admin/cms/banners")]  // canonical
[Produces("application/json")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class AdminCmsBannersController : ControllerBase
{
    private readonly ICmsBannerService _service;
    public AdminCmsBannersController(ICmsBannerService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CmsBannerResponse>>> List(CancellationToken ct)
        => (await _service.ListAsync(activeOnly: false, ct)).ToActionResult();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CmsBannerResponse>> Get(int id, CancellationToken ct)
        => (await _service.GetAsync(id, ct)).ToActionResult();

    [HttpPost]
    public async Task<ActionResult<CmsBannerResponse>> Create(
        [FromBody] UpsertCmsBannerRequest body, CancellationToken ct)
        => (await _service.CreateAsync(body, ct)).ToActionResult();

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CmsBannerResponse>> Update(
        int id, [FromBody] UpsertCmsBannerRequest body, CancellationToken ct)
        => (await _service.UpdateAsync(id, body, ct)).ToActionResult();

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id, CancellationToken ct)
        => (await _service.DeleteAsync(id, ct)).ToActionResult();
}
