using CareerPlatform.Api.Features.Assessments.Dto;
using CareerPlatform.Api.Features.Assessments.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CareerPlatform.Api.Features.Assessments.Controller;

/// <summary>
/// The live attempt runner: load, autosave, trial-run, submit, and read the scorecard.
///
/// Kept separate from <see cref="AssessmentsController"/> because the catalog controller is a thin
/// CRUD surface while this one is the exam runtime — different lifecycle, different rate-limiting
/// profile, and the only place a score may be produced. Ownership of the attempt is verified inside
/// <see cref="IAssessmentRunnerService"/>, so every route here is safe against an attempt id
/// belonging to another student.
/// </summary>
[ApiController]
[Route("api/v1/assessments/attempts/{attemptId:int}")]  // canonical
[Produces("application/json")]
[Authorize]
public sealed class AssessmentRunnerController : ControllerBase
{
    private readonly IAssessmentRunnerService _runner;

    public AssessmentRunnerController(IAssessmentRunnerService runner) => _runner = runner;

    /// <summary>Loads the attempt with its questions, saved answers, and remaining time.</summary>
    [HttpGet("runner")]
    public async Task<ActionResult<AttemptRunnerResponse>> GetRunner(int attemptId, CancellationToken ct)
        => (await _runner.GetRunnerAsync(attemptId, ct)).ToActionResult();

    /// <summary>Upserts the draft answer for a single question (autosave).</summary>
    [HttpPut("answers")]
    public async Task<ActionResult> SaveAnswer(
        int attemptId, [FromBody] SaveAnswerRequest body, CancellationToken ct)
        => (await _runner.SaveAnswerAsync(attemptId, body, ct)).ToActionResult();

    /// <summary>Runs the code against the visible sample cases only.</summary>
    [HttpPost("run")]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult<RunCodeResponse>> Run(
        int attemptId, [FromBody] RunCodeRequest body, CancellationToken ct)
        => (await _runner.RunSampleTestsAsync(attemptId, body, ct)).ToActionResult();

    /// <summary>Finalises the attempt and grades it server-side.</summary>
    [HttpPost("submit")]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult<AttemptScorecardResponse>> Submit(int attemptId, CancellationToken ct)
        => (await _runner.SubmitAsync(attemptId, ct)).ToActionResult();

    /// <summary>Returns the graded scorecard for a submitted attempt.</summary>
    [HttpGet("scorecard")]
    public async Task<ActionResult<AttemptScorecardResponse>> Scorecard(int attemptId, CancellationToken ct)
        => (await _runner.GetScorecardAsync(attemptId, ct)).ToActionResult();
}
