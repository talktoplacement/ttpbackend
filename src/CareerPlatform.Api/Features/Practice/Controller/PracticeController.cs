using CareerPlatform.Api.Features.Practice.Dto;
using CareerPlatform.Api.Features.Practice.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CareerPlatform.Api.Features.Practice.Controller;

/// <summary>Public practice-question catalog + student bookmarks.</summary>
[ApiController]
[Route("api/v1/practice")]  // canonical
[Produces("application/json")]
public sealed class PracticeController : ControllerBase
{
    private readonly IPracticeService _service;
    public PracticeController(IPracticeService service) => _service = service;

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<PracticeQuestionResponse>>> List(
        [FromQuery] string? category, CancellationToken ct)
        => (await _service.ListAsync(category, publishedOnly: true, ct)).ToActionResult();

    [HttpGet("{slug}")]
    [AllowAnonymous]
    public async Task<ActionResult<PracticeQuestionResponse>> Get(string slug, CancellationToken ct)
        => (await _service.GetAsync(slug, ct)).ToActionResult();

    [HttpGet("bookmarks/me")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<PracticeBookmarkResponse>>> ListMyBookmarks(CancellationToken ct)
        => (await _service.ListMyBookmarksAsync(ct)).ToActionResult();

    [HttpPost("bookmarks/{questionId:int}")]
    [Authorize]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult<PracticeBookmarkResponse>> AddBookmark(
        int questionId, [FromBody] ToggleBookmarkRequest? body, CancellationToken ct)
        => (await _service.AddBookmarkAsync(questionId, body?.Notes, ct)).ToActionResult();

    [HttpDelete("bookmarks/{questionId:int}")]
    [Authorize]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult> RemoveBookmark(int questionId, CancellationToken ct)
        => (await _service.RemoveBookmarkAsync(questionId, ct)).ToActionResult();
}

/// <summary>Admin CRUD for practice questions.</summary>
[ApiController]
[Route("api/v1/admin/practice")]  // canonical
[Produces("application/json")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class AdminPracticeController : ControllerBase
{
    private readonly IPracticeService _service;
    public AdminPracticeController(IPracticeService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PracticeQuestionResponse>>> List(
        [FromQuery] string? category, CancellationToken ct)
        => (await _service.ListAsync(category, publishedOnly: false, ct)).ToActionResult();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PracticeQuestionResponse>> GetById(int id, CancellationToken ct)
        => (await _service.GetByIdAsync(id, ct)).ToActionResult();

    [HttpPost]
    public async Task<ActionResult<PracticeQuestionResponse>> Create(
        [FromBody] CreatePracticeQuestionRequest body, CancellationToken ct)
        => (await _service.CreateAsync(body, ct)).ToActionResult();

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PracticeQuestionResponse>> Update(
        int id, [FromBody] UpdatePracticeQuestionRequest body, CancellationToken ct)
        => (await _service.UpdateAsync(id, body, ct)).ToActionResult();

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id, CancellationToken ct)
        => (await _service.DeleteAsync(id, ct)).ToActionResult();
}
