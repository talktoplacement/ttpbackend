using CareerPlatform.Api.Features.Reviews.Dto;
using CareerPlatform.Api.Features.Reviews.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CareerPlatform.Api.Features.Reviews.Controller;

/// <summary>Public: read approved reviews for a course. Authenticated: submit a review.</summary>
[ApiController]
[Route("api/v1/reviews")]
[Produces("application/json")]
public sealed class ReviewsController : ControllerBase
{
    private readonly IReviewService _service;
    public ReviewsController(IReviewService service) => _service = service;

    /// <summary>GET /courses/{id} — approved reviews on the given course.</summary>
    [HttpGet("courses/{courseId:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<ReviewResponse>>> ListForCourse(
        int courseId, CancellationToken ct)
        => (await _service.ListPublicForCourseAsync(courseId, ct)).ToActionResult();

    /// <summary>POST — submit a review. Enters admin moderation queue.</summary>
    [HttpPost]
    [Authorize]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult<ReviewResponse>> Create(
        [FromBody] CreateReviewRequest body, CancellationToken ct)
        => (await _service.CreateAsync(body, ct)).ToActionResult();
}

/// <summary>Admin moderation queue.</summary>
[ApiController]
[Route("api/v1/admin/reviews")]
[Produces("application/json")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class AdminReviewsController : ControllerBase
{
    private readonly IReviewService _service;
    public AdminReviewsController(IReviewService service) => _service = service;

    /// <summary>GET ?status=pending|approved|rejected — filter the moderation queue.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ReviewResponse>>> List(
        [FromQuery] string? status, CancellationToken ct)
        => (await _service.ListForAdminAsync(status, ct)).ToActionResult();

    /// <summary>POST /{id}/moderate — approve or reject.</summary>
    [HttpPost("{id:int}/moderate")]
    public async Task<ActionResult<ReviewResponse>> Moderate(
        int id, [FromBody] ModerateReviewRequest body, CancellationToken ct)
        => (await _service.ModerateAsync(id, body, ct)).ToActionResult();
}
