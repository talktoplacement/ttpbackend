using CareerPlatform.Api.Features.LearningPaths.Dto;
using CareerPlatform.Api.Features.LearningPaths.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CareerPlatform.Api.Features.LearningPaths.Controller;

/// <summary>Public learning-paths catalog.</summary>
[ApiController]
[Route("api/v1/learning-paths")]   // canonical
[Produces("application/json")]
public sealed class LearningPathsController : ControllerBase
{
    private readonly ILearningPathService _service;
    public LearningPathsController(ILearningPathService service) => _service = service;

    /// <summary>GET — published paths, newest-first.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<LearningPathResponse>>> List(
        [FromQuery] string? targetRole, CancellationToken ct)
    {
        var result = await _service.ListAsync(targetRole, publishedOnly: true, ct);
        return result.ToActionResult();
    }

    /// <summary>GET /{slug} — a single published path.</summary>
    [HttpGet("{slug}")]
    [AllowAnonymous]
    public async Task<ActionResult<LearningPathResponse>> Get(string slug, CancellationToken ct)
    {
        var result = await _service.GetAsync(slug, ct);
        return result.ToActionResult();
    }
}

/// <summary>Admin CRUD for learning paths.</summary>
[ApiController]
[Route("api/v1/admin/learning-paths")]  // canonical
[Produces("application/json")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class AdminLearningPathsController : ControllerBase
{
    private readonly ILearningPathService _service;
    public AdminLearningPathsController(ILearningPathService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LearningPathResponse>>> List(
        [FromQuery] string? targetRole, CancellationToken ct)
    {
        var result = await _service.ListAsync(targetRole, publishedOnly: false, ct);
        return result.ToActionResult();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LearningPathResponse>> GetById(int id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<ActionResult<LearningPathResponse>> Create(
        [FromBody] CreateLearningPathRequest body, CancellationToken ct)
    {
        var result = await _service.CreateAsync(body, ct);
        return result.ToActionResult();
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<LearningPathResponse>> Update(
        int id, [FromBody] UpdateLearningPathRequest body, CancellationToken ct)
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
