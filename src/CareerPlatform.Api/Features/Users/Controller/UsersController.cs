using CareerPlatform.Api.Features.Users.Dto;
using CareerPlatform.Api.Features.Users.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CareerPlatform.Api.Features.Users.Controller;

/// <summary>Self-service profile: read + update + password + sync.</summary>
[ApiController]
[Route("api/v1/me")]  // canonical
[Produces("application/json")]
[Authorize]
public sealed class UsersController : ControllerBase
{
    private readonly IUserService _service;
    public UsersController(IUserService service) => _service = service;

    /// <summary>GET — the authenticated user's own profile.</summary>
    [HttpGet]
    public async Task<ActionResult<MyProfileResponse>> Get(CancellationToken ct)
    {
        var result = await _service.GetMineAsync(ct);
        return result.ToActionResult();
    }

    /// <summary>PUT — update the authenticated user's own editable fields.</summary>
    [HttpPut]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult<MyProfileResponse>> Update(
        [FromBody] UpdateMyProfileRequest body, CancellationToken ct)
    {
        var result = await _service.UpdateMineAsync(body, ct);
        return result.ToActionResult();
    }

    /// <summary>POST /password — change the authenticated user's password (current+new).</summary>
    [HttpPost("password")]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult> ChangePassword(
        [FromBody] ChangeMyPasswordRequest body, CancellationToken ct)
    {
        var result = await _service.ChangeMyPasswordAsync(body.CurrentPassword, body.NewPassword, ct);
        return result.ToActionResult();
    }
}

/// <summary>Auth-sync endpoint — ensures a UserProfile row exists for the authenticated JWT subject.</summary>
[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
[Authorize]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class AuthSyncController : ControllerBase
{
    private readonly IUserService _service;
    public AuthSyncController(IUserService service) => _service = service;

    /// <summary>POST /sync — ensures a UserProfile row exists for the authenticated JWT subject.</summary>
    [HttpPost("sync")]
    public async Task<ActionResult<MyProfileResponse>> Sync(
        [FromBody] SyncMyProfileRequest? body, CancellationToken ct)
    {
        var result = await _service.SyncAsync(body?.DisplayName, ct);
        return result.ToActionResult();
    }
}
