using CareerPlatform.Api.Features.Interviews.Dto;
using CareerPlatform.Api.Features.Interviews.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CareerPlatform.Api.Features.Interviews.Controller;

/// <summary>Public interview-question catalog.</summary>
[ApiController]
[Route("api/v1/interview-questions")]  // canonical
[Produces("application/json")]
public sealed class InterviewQuestionsController : ControllerBase
{
    private readonly IInterviewService _service;
    public InterviewQuestionsController(IInterviewService service) => _service = service;

    /// <summary>
    /// GET /topics — the published bank grouped into topics with real question counts, the company
    /// tags actually present, and the caller's own session history per topic.
    ///
    /// Requires authentication (unlike the question list) because the response carries the caller's
    /// personal session counts and best score.
    /// </summary>
    [HttpGet("topics")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<InterviewTopicResponse>>> Topics(CancellationToken ct)
        => (await _service.ListTopicsAsync(ct)).ToActionResult();

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<InterviewQuestionResponse>>> List(
        [FromQuery] string? topic, [FromQuery] string? difficulty, CancellationToken ct)
        => (await _service.ListQuestionsAsync(topic, difficulty, publishedOnly: true, ct)).ToActionResult();
}

/// <summary>Admin CRUD for interview questions.</summary>
[ApiController]
[Route("api/v1/admin/interview-questions")]  // canonical
[Produces("application/json")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class AdminInterviewQuestionsController : ControllerBase
{
    private readonly IInterviewService _service;
    public AdminInterviewQuestionsController(IInterviewService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InterviewQuestionResponse>>> List(
        [FromQuery] string? topic, [FromQuery] string? difficulty, CancellationToken ct)
        => (await _service.ListQuestionsAsync(topic, difficulty, publishedOnly: false, ct)).ToActionResult();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<InterviewQuestionResponse>> GetById(int id, CancellationToken ct)
        => (await _service.GetQuestionByIdAsync(id, ct)).ToActionResult();

    [HttpPost]
    public async Task<ActionResult<InterviewQuestionResponse>> Create(
        [FromBody] CreateInterviewQuestionRequest body, CancellationToken ct)
        => (await _service.CreateQuestionAsync(body, ct)).ToActionResult();

    [HttpPut("{id:int}")]
    public async Task<ActionResult<InterviewQuestionResponse>> Update(
        int id, [FromBody] UpdateInterviewQuestionRequest body, CancellationToken ct)
        => (await _service.UpdateQuestionAsync(id, body, ct)).ToActionResult();

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id, CancellationToken ct)
        => (await _service.DeleteQuestionAsync(id, ct)).ToActionResult();
}

/// <summary>Student mock-interview sessions ("me" scope).</summary>
[ApiController]
[Route("api/v1/interviews/sessions/me")]  // canonical
[Produces("application/json")]
[Authorize]
public sealed class MyInterviewSessionsController : ControllerBase
{
    private readonly IInterviewService _service;
    public MyInterviewSessionsController(IInterviewService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MockInterviewSessionResponse>>> List(CancellationToken ct)
        => (await _service.ListMySessionsAsync(ct)).ToActionResult();

    [HttpPost]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult<MockInterviewSessionResponse>> Create(
        [FromBody] CreateInterviewSessionRequest body, CancellationToken ct)
        => (await _service.CreateMySessionAsync(body, ct)).ToActionResult();

    [HttpPut("{id:int}")]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult<MockInterviewSessionResponse>> Update(
        int id, [FromBody] UpdateInterviewSessionRequest body, CancellationToken ct)
        => (await _service.UpdateMySessionAsync(id, body, ct)).ToActionResult();
}

/// <summary>Admin oversight of every student's mock-interview sessions.</summary>
[ApiController]
[Route("api/v1/admin/interviews/sessions")]
[Produces("application/json")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class AdminInterviewSessionsController : ControllerBase
{
    private readonly IInterviewService _service;
    public AdminInterviewSessionsController(IInterviewService service) => _service = service;

    /// <summary>GET ?status=&amp;topic= — filterable list across all students.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminMockInterviewSessionResponse>>> List(
        [FromQuery] string? status, [FromQuery] string? topic, CancellationToken ct)
        => (await _service.ListAllSessionsAsync(status, topic, ct)).ToActionResult();
}
