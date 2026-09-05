using CareerPlatform.Api.Features.CodeExecution.Dto;

namespace CareerPlatform.Api.Features.CodeExecution.Service;

/// <summary>
/// Ad-hoc ("scratchpad") code execution for the documentation code viewer.
///
/// Distinct from the assessment runner: there is no attempt, no question, and no grading, so nothing
/// here may read or write assessment state. It exists only so a code sample in an article can
/// actually be run. Keeping it on its own service means the grader's invariants — hidden test cases,
/// server-side scoring — cannot be reached through this surface.
/// </summary>
public interface ICodeRunnerService
{
    /// <summary>Languages available to run, plus whether a sandbox is configured at all.</summary>
    RunnableLanguageCatalogResponse GetCatalog();

    /// <summary>Runs a snippet against the supplied stdin and returns its captured output.</summary>
    Task<Result<CodeRunResponse>> RunAsync(CodeRunRequest request, CancellationToken ct);
}
