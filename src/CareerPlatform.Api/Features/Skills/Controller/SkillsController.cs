using CareerPlatform.Api.Features.Skills.Dto;
using CareerPlatform.Api.Features.Skills.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CareerPlatform.Api.Features.Skills.Controller;

/// <summary>Student self-service skills matrix on the caller's profile.</summary>
[ApiController]
[Route("api/v1/me/skills")]
[Produces("application/json")]
[Authorize]
public sealed class MySkillsController : ControllerBase
{
    private readonly ISkillService _service;
    public MySkillsController(ISkillService service) => _service = service;

    /// <summary>GET — grouped by category.</summary>
    [HttpGet]
    public async Task<ActionResult<SkillsResponse>> Get(CancellationToken ct)
        => (await _service.GetMySkillsAsync(ct)).ToActionResult();

    /// <summary>PUT — replaces the caller's entire skill list. Empty list = clear all.</summary>
    [HttpPut]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult<SkillsResponse>> Replace(
        [FromBody] ReplaceSkillsRequest body, CancellationToken ct)
        => (await _service.ReplaceMySkillsAsync(body, ct)).ToActionResult();
}
