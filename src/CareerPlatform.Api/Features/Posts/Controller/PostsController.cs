using CareerPlatform.Api.Features.Posts.Dto;
using CareerPlatform.Api.Features.Posts.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CareerPlatform.Api.Features.Posts.Controller;

/// <summary>
/// Author surface — any authenticated user can write, save drafts, and submit for review.
/// "Save for later" = POST (create) / PUT (update); "Submit for Review" = POST /{id}/submit.
/// </summary>
[ApiController]
[Route("api/v1/posts")]
[Produces("application/json")]
[Authorize]
public sealed class PostsController : ControllerBase
{
    private readonly IPostService _service;
    public PostsController(IPostService service) => _service = service;

    /// <summary>GET /me — the caller's own posts (all statuses), newest-first.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<IReadOnlyList<PostSummaryResponse>>> ListMine(CancellationToken ct)
        => (await _service.ListMineAsync(ct)).ToActionResult();

    /// <summary>GET /me/{id} — one of the caller's own posts (any status), for the editor.</summary>
    [HttpGet("me/{id:int}")]
    public async Task<ActionResult<PostResponse>> GetMine(int id, CancellationToken ct)
        => (await _service.GetMineAsync(id, ct)).ToActionResult();

    /// <summary>POST — create a new draft ("Save for later" on a fresh post).</summary>
    [HttpPost]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult<PostResponse>> Create(
        [FromBody] PostEditorRequest body, CancellationToken ct)
        => (await _service.CreateAsync(body, ct)).ToActionResult();

    /// <summary>PUT /{id} — update a draft/rejected post ("Save for later" on an existing post).</summary>
    [HttpPut("{id:int}")]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult<PostResponse>> Update(
        int id, [FromBody] PostEditorRequest body, CancellationToken ct)
        => (await _service.UpdateAsync(id, body, ct)).ToActionResult();

    /// <summary>POST /{id}/submit — "Submit for Review": moves draft/rejected → in_review.</summary>
    [HttpPost("{id:int}/submit")]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult<PostResponse>> Submit(int id, CancellationToken ct)
        => (await _service.SubmitAsync(id, ct)).ToActionResult();

    /// <summary>DELETE /{id} — author removes their own non-published post.</summary>
    [HttpDelete("{id:int}")]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult> Delete(int id, CancellationToken ct)
        => (await _service.DeleteAsync(id, ct)).ToActionResult();
}

/// <summary>Public reading surface — only published posts are exposed here.</summary>
[ApiController]
[Route("api/v1/posts/published")]
[Produces("application/json")]
[AllowAnonymous]
public sealed class PublicPostsController : ControllerBase
{
    private readonly IPostService _service;
    public PublicPostsController(IPostService service) => _service = service;

    /// <summary>GET — published posts, optionally filtered by tag, newest-first.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PostSummaryResponse>>> List(
        [FromQuery] string? tag, CancellationToken ct)
        => (await _service.ListPublishedAsync(tag, ct)).ToActionResult();

    /// <summary>GET /{slug} — a single published post by slug.</summary>
    [HttpGet("{slug}")]
    public async Task<ActionResult<PostResponse>> Get(string slug, CancellationToken ct)
        => (await _service.GetPublishedBySlugAsync(slug, ct)).ToActionResult();
}

/// <summary>Admin review queue — approve or reject submitted posts.</summary>
[ApiController]
[Route("api/v1/admin/posts")]
[Produces("application/json")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class AdminPostsController : ControllerBase
{
    private readonly IPostService _service;
    public AdminPostsController(IPostService service) => _service = service;

    /// <summary>GET — review queue. Defaults to in_review; pass ?status= to filter any state.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PostSummaryResponse>>> List(
        [FromQuery] string? status, CancellationToken ct)
        => (await _service.ListForReviewAsync(status, ct)).ToActionResult();

    /// <summary>GET /{id} — full post (any status) for the reviewer / editor.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PostResponse>> Get(int id, CancellationToken ct)
        => (await _service.GetForReviewAsync(id, ct)).ToActionResult();

    /// <summary>POST /{id}/review — approve (→ published) or reject (→ rejected, with a note).</summary>
    [HttpPost("{id:int}/review")]
    public async Task<ActionResult<PostResponse>> Review(
        int id, [FromBody] ReviewPostRequest body, CancellationToken ct)
        => (await _service.ReviewAsync(id, body, ct)).ToActionResult();

    // ── Admin "Post Article": write, save draft, publish directly (no review) ──

    /// <summary>POST — admin creates an article (starts as a draft they own).</summary>
    [HttpPost]
    public async Task<ActionResult<PostResponse>> Create(
        [FromBody] PostEditorRequest body, CancellationToken ct)
        => (await _service.AdminCreateAsync(body, ct)).ToActionResult();

    /// <summary>PUT /{id} — admin edits any article; edits to a published article go live immediately.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<PostResponse>> Update(
        int id, [FromBody] PostEditorRequest body, CancellationToken ct)
        => (await _service.AdminUpdateAsync(id, body, ct)).ToActionResult();

    /// <summary>POST /{id}/publish — publish directly to the public view (skips review).</summary>
    [HttpPost("{id:int}/publish")]
    public async Task<ActionResult<PostResponse>> Publish(int id, CancellationToken ct)
        => (await _service.AdminPublishAsync(id, ct)).ToActionResult();

    /// <summary>POST /{id}/unpublish — pull an article back off the public view (→ draft).</summary>
    [HttpPost("{id:int}/unpublish")]
    public async Task<ActionResult<PostResponse>> Unpublish(int id, CancellationToken ct)
        => (await _service.AdminUnpublishAsync(id, ct)).ToActionResult();

    /// <summary>DELETE /{id} — admin removes any article regardless of status.</summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id, CancellationToken ct)
        => (await _service.AdminDeleteAsync(id, ct)).ToActionResult();
}
