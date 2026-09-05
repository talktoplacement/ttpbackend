using CareerPlatform.Api.Features.Resumes.Dto;
using CareerPlatform.Api.Features.Resumes.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CareerPlatform.Api.Features.Resumes.Controller;

/// <summary>
/// Resume drafts owned by the caller. Nested under the existing <c>/api/v1/resumes/me</c> prefix so
/// the whole student resume surface lives on one route family.
/// </summary>
[ApiController]
[Route("api/v1/resumes/me/drafts")]
[Produces("application/json")]
[Authorize]
public sealed class MyResumeDraftsController : ControllerBase
{
    private readonly IResumeDraftService _service;
    public MyResumeDraftsController(IResumeDraftService service) => _service = service;

    /// <summary>GET — the caller's drafts, most recently edited first.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ResumeDraftResponse>>> List(CancellationToken ct)
        => (await _service.ListMineAsync(ct)).ToActionResult();

    /// <summary>GET /{id} — one draft, including its full builder document.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ResumeDraftResponse>> Get(int id, CancellationToken ct)
        => (await _service.GetMineAsync(id, ct)).ToActionResult();

    /// <summary>POST — starts a new draft against a published template.</summary>
    [HttpPost]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult<ResumeDraftResponse>> Create(
        [FromBody] CreateResumeDraftRequest body, CancellationToken ct)
        => (await _service.CreateMineAsync(body, ct)).ToActionResult();

    /// <summary>
    /// PUT /{id} — saves a draft. Fields are individually optional so the builder can autosave only
    /// what changed without having to resend the whole document.
    /// </summary>
    [HttpPut("{id:int}")]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult<ResumeDraftResponse>> Update(
        int id, [FromBody] UpdateResumeDraftRequest body, CancellationToken ct)
        => (await _service.UpdateMineAsync(id, body, ct)).ToActionResult();

    /// <summary>DELETE /{id} — discards a draft.</summary>
    [HttpDelete("{id:int}")]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult> Delete(int id, CancellationToken ct)
        => (await _service.DeleteMineAsync(id, ct)).ToActionResult();
}
