using CareerPlatform.Api.Configuration;
using CareerPlatform.Api.Features.CodeExecution.Dto;
using CareerPlatform.Api.Infrastructure.CodeExecution;

namespace CareerPlatform.Api.Features.CodeExecution.Service;

internal sealed class CodeRunnerService : ICodeRunnerService
{
    private readonly ICodeExecutor _executor;
    private readonly CodeExecutionOptions _options;

    public CodeRunnerService(ICodeExecutor executor, IOptions<CodeExecutionOptions> options)
    {
        _executor = executor;
        _options = options.Value;
    }

    public RunnableLanguageCatalogResponse GetCatalog() => new(
        _executor.IsEnabled,
        _executor.SupportedLanguages
            .Select(l => new RunnableLanguageResponse(l.Id, l.Label))
            .ToList());

    public async Task<Result<CodeRunResponse>> RunAsync(CodeRunRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_executor.IsEnabled)
        {
            // Not an error: the client asks for the catalog first and hides Run when disabled. A
            // direct caller gets a clear, non-exceptional answer.
            return Result.Success(new CodeRunResponse(
                ExecutionAvailable: false,
                Succeeded: false,
                Stdout: string.Empty,
                Stderr: string.Empty,
                TimedOut: false,
                FailureReason: "Code execution is not available on this environment."));
        }

        // The language must be one the sandbox actually offers. Checked here rather than in the
        // validator because the catalog is runtime state (it depends on the configured executor),
        // and a stale client should get a precise 400 instead of a generic sandbox failure.
        var isSupported = _executor.SupportedLanguages
            .Any(l => string.Equals(l.Id, request.Language, StringComparison.OrdinalIgnoreCase));
        if (!isSupported)
        {
            return Result.Failure<CodeRunResponse>(Error.Validation(
                "CodeExecution.UnsupportedLanguage",
                $"'{request.Language}' is not an available language. " +
                "Fetch the language catalog and choose one of the offered ids."));
        }

        var execution = await _executor.ExecuteAsync(
            new CodeExecutionRequest(
                request.Language,
                request.SourceCode,
                request.Stdin ?? string.Empty,
                _options.ScratchpadTimeLimitMs),
            ct);

        return Result.Success(new CodeRunResponse(
            ExecutionAvailable: true,
            Succeeded: execution.Succeeded,
            Stdout: execution.Stdout,
            Stderr: execution.Stderr,
            TimedOut: execution.TimedOut,
            FailureReason: execution.FailureReason));
    }
}
