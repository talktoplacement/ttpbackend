using CareerPlatform.Api.Features.CodeExecution.Dto;
using CareerPlatform.Api.Features.CodeExecution.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CareerPlatform.Api.Features.CodeExecution.Controller;

/// <summary>
/// Ad-hoc code execution for the documentation code viewer.
///
/// <c>[Authorize]</c> is not optional here: this endpoint runs caller-supplied code in a sandbox, so
/// anonymous access would hand the world a free compute service. It also carries the dedicated
/// <see cref="RateLimitPolicy.CodeExecution"/> budget, which bounds sandbox processes per user rather
/// than plain request volume.
/// </summary>
[ApiController]
[Route("api/v1/code-execution")]  // canonical
[Produces("application/json")]
[Authorize]
public sealed class CodeExecutionController : ControllerBase
{
    private readonly ICodeRunnerService _runner;

    public CodeExecutionController(ICodeRunnerService runner) => _runner = runner;

    /// <summary>
    /// GET /languages — what can be run, and whether a sandbox exists at all. The client calls this
    /// before showing a Run action so it never offers a button that cannot work.
    /// </summary>
    [HttpGet("languages")]
    public ActionResult<RunnableLanguageCatalogResponse> Languages() => Ok(_runner.GetCatalog());

    /// <summary>POST /run — executes a snippet and returns its captured output.</summary>
    [HttpPost("run")]
    [EnableRateLimiting(RateLimitPolicy.CodeExecution)]
    public async Task<ActionResult<CodeRunResponse>> Run(
        [FromBody] CodeRunRequest body, CancellationToken ct)
        => (await _runner.RunAsync(body, ct)).ToActionResult();
}
