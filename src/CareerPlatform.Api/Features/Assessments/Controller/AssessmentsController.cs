using CareerPlatform.Api.Features.Assessments.Dto;
using CareerPlatform.Api.Features.Assessments.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CareerPlatform.Api.Features.Assessments.Controller;

/// <summary>Student-facing assessments catalog + attempt workflow.</summary>
[ApiController]
[Route("api/v1/assessments")]  // canonical
[Produces("application/json")]
[Authorize]
public sealed class AssessmentsController : ControllerBase
{
    private readonly IAssessmentService _service;
    public AssessmentsController(IAssessmentService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AssessmentResponse>>> List(
        [FromQuery] string? category, CancellationToken ct)
        => (await _service.ListAsync(category, publishedOnly: true, ct)).ToActionResult();

    [HttpGet("{slug}")]
    public async Task<ActionResult<AssessmentResponse>> Get(string slug, CancellationToken ct)
        => (await _service.GetAsync(slug, ct)).ToActionResult();

    [HttpGet("attempts/me")]
    public async Task<ActionResult<IReadOnlyList<AssessmentAttemptResponse>>> ListMyAttempts(CancellationToken ct)
        => (await _service.ListMyAttemptsAsync(ct)).ToActionResult();

    [HttpGet("attempts/{id:int}")]
    public async Task<ActionResult<AssessmentAttemptResponse>> GetMyAttempt(int id, CancellationToken ct)
        => (await _service.GetMyAttemptAsync(id, ct)).ToActionResult();

    [HttpPost("{slug}/attempts")]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult<AssessmentAttemptResponse>> StartAttempt(string slug, CancellationToken ct)
        => (await _service.StartAttemptAsync(slug, ct)).ToActionResult();

    // Submission is POST api/v1/assessments/attempts/{attemptId}/submit on AssessmentRunnerController.
}

/// <summary>Admin CRUD for assessments.</summary>
[ApiController]
[Route("api/v1/admin/assessments")]  // canonical
[Produces("application/json")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class AdminAssessmentsController : ControllerBase
{
    private readonly IAssessmentService _service;
    public AdminAssessmentsController(IAssessmentService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AssessmentResponse>>> List(
        [FromQuery] string? category, CancellationToken ct)
        => (await _service.ListAsync(category, publishedOnly: false, ct)).ToActionResult();

    /// <summary>Loads a single assessment by id, drafts included.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AssessmentResponse>> Get(int id, CancellationToken ct)
        => (await _service.GetByIdAsync(id, ct)).ToActionResult();

    [HttpPost]
    public async Task<ActionResult<AssessmentResponse>> Create(
        [FromBody] CreateAssessmentRequest body, CancellationToken ct)
        => (await _service.CreateAsync(body, ct)).ToActionResult();

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AssessmentResponse>> Update(
        int id, [FromBody] UpdateAssessmentRequest body, CancellationToken ct)
        => (await _service.UpdateAsync(id, body, ct)).ToActionResult();

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id, CancellationToken ct)
        => (await _service.DeleteAsync(id, ct)).ToActionResult();
}

/// <summary>
/// Admin authoring of an assessment's question bank. Isolated from
/// <see cref="AdminAssessmentsController"/> because these are the only routes that carry the answer
/// key over the wire.
/// </summary>
[ApiController]
[Route("api/v1/admin/assessments/{assessmentId:int}/questions")]  // canonical
[Produces("application/json")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class AdminAssessmentQuestionsController : ControllerBase
{
    private readonly IAssessmentAuthoringService _authoring;

    public AdminAssessmentQuestionsController(IAssessmentAuthoringService authoring)
        => _authoring = authoring;

    /// <summary>Returns the bank with correct options and hidden test cases.</summary>
    [HttpGet]
    public async Task<ActionResult<QuestionBankResponse>> Get(int assessmentId, CancellationToken ct)
        => (await _authoring.GetBankAsync(assessmentId, ct)).ToActionResult();

    /// <summary>Replaces the bank atomically and re-derives the assessment totals.</summary>
    [HttpPut]
    public async Task<ActionResult<QuestionBankResponse>> Replace(
        int assessmentId, [FromBody] ReplaceQuestionBankRequest body, CancellationToken ct)
        => (await _authoring.ReplaceBankAsync(assessmentId, body, ct)).ToActionResult();
}
