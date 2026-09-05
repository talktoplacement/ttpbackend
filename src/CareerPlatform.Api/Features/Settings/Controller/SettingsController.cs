using CareerPlatform.Api.Features.Settings.Dto;
using CareerPlatform.Api.Features.Settings.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CareerPlatform.Api.Features.Settings.Controller;

[ApiController]
[Route("api/v1/admin/settings")]   // canonical
[Produces("application/json")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class SettingsController : ControllerBase
{
    private readonly ISettingsService _service;
    public SettingsController(ISettingsService service) => _service = service;

    /// <summary>GET — every platform setting, optionally filtered by <c>?category=</c>.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PlatformSettingResponse>>> List(
        [FromQuery] string? category, CancellationToken ct)
    {
        var result = await _service.ListAsync(category, ct);
        return result.ToActionResult();
    }

    /// <summary>PUT — apply a batch of key/value updates; returns the full fresh settings list.</summary>
    [HttpPut]
    public async Task<ActionResult<IReadOnlyList<PlatformSettingResponse>>> Update(
        [FromBody] UpdateSettingsRequest body, CancellationToken ct)
    {
        var result = await _service.UpdateAsync(body.Updates ?? Array.Empty<SettingUpdate>(), ct);
        return result.ToActionResult();
    }
}
