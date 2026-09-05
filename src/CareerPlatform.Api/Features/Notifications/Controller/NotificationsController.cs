using CareerPlatform.Api.Features.Notifications.Dto;
using CareerPlatform.Api.Features.Notifications.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CareerPlatform.Api.Features.Notifications.Controller;

[ApiController]
[Route("api/v1/notifications")]   // canonical
[Produces("application/json")]
[Authorize]
public sealed class NotificationsController : ControllerBase
{
    private readonly INotificationService _service;
    public NotificationsController(INotificationService service) => _service = service;

    /// <summary>GET /me — the authenticated user's non-dismissed feed.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<IReadOnlyList<NotificationResponse>>> ListMine(CancellationToken ct)
    {
        var result = await _service.ListMineAsync(ct);
        return result.ToActionResult();
    }

    /// <summary>POST /{id}/read — mark a single notification read.</summary>
    [HttpPost("{id:int}/read")]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult> MarkRead(int id, CancellationToken ct)
    {
        var result = await _service.MarkReadAsync(id, ct);
        return result.ToActionResult();
    }

    /// <summary>POST /read-all — mark every unread notification read.</summary>
    [HttpPost("read-all")]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult> MarkAllRead(CancellationToken ct)
    {
        var result = await _service.MarkAllReadAsync(ct);
        return result.ToActionResult();
    }

    /// <summary>POST /clear-all — soft-dismiss every notification.</summary>
    [HttpPost("clear-all")]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult> ClearAll(CancellationToken ct)
    {
        var result = await _service.ClearAllAsync(ct);
        return result.ToActionResult();
    }
}

/// <summary>Admin fan-out publish.</summary>
[ApiController]
[Route("api/v1/admin/notifications")]      // canonical
[Produces("application/json")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class AdminNotificationsController : ControllerBase
{
    private readonly INotificationService _service;
    public AdminNotificationsController(INotificationService service) => _service = service;

    /// <summary>POST — fan out one notification row per targeted user (REST create on the collection).</summary>
    [HttpPost]
    public async Task<ActionResult<PublishNotificationResult>> Publish(
        [FromBody] PublishNotificationRequest body, CancellationToken ct)
    {
        var result = await _service.PublishAsync(body, ct);
        return result.ToActionResult();
    }
}
