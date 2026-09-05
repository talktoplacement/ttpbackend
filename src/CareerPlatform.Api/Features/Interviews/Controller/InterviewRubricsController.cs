using CareerPlatform.Api.Features.Interviews.Dto;
using CareerPlatform.Api.Features.Interviews.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CareerPlatform.Api.Features.Interviews.Controller;

/// <summary>Public: the published grading-rubric axes.</summary>
[ApiController]
[Route("api/v1/interview-rubrics")]  // canonical
[Produces("application/json")]
public sealed class InterviewRubricsController : ControllerBase
{
    private readonly IInterviewRubricService _service;
    public InterviewRubricsController(IInterviewRubricService service) => _service = service;

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<InterviewRubricResponse>>> List(CancellationToken ct)
        => (await _service.ListAsync(publishedOnly: true, ct)).ToActionResult();
}

/// <summary>Admin CRUD for grading-rubric axes.</summary>
[ApiController]
[Route("api/v1/admin/interview-rubrics")]  // canonical
[Produces("application/json")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class AdminInterviewRubricsController : ControllerBase
{
    private readonly IInterviewRubricService _service;
    public AdminInterviewRubricsController(IInterviewRubricService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InterviewRubricResponse>>> List(CancellationToken ct)
        => (await _service.ListAsync(publishedOnly: false, ct)).ToActionResult();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<InterviewRubricResponse>> Get(int id, CancellationToken ct)
        => (await _service.GetAsync(id, ct)).ToActionResult();

    [HttpPost]
    public async Task<ActionResult<InterviewRubricResponse>> Create(
        [FromBody] UpsertInterviewRubricRequest body, CancellationToken ct)
        => (await _service.CreateAsync(body, ct)).ToActionResult();

    [HttpPut("{id:int}")]
    public async Task<ActionResult<InterviewRubricResponse>> Update(
        int id, [FromBody] UpsertInterviewRubricRequest body, CancellationToken ct)
        => (await _service.UpdateAsync(id, body, ct)).ToActionResult();

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id, CancellationToken ct)
        => (await _service.DeleteAsync(id, ct)).ToActionResult();
}
