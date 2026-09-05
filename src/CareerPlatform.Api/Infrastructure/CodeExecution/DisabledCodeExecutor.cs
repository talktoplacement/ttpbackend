namespace CareerPlatform.Api.Infrastructure.CodeExecution;

/// <summary>
/// Null-object <see cref="ICodeExecutor"/> used when no sandbox is configured.
///
/// Chosen over leaving the dependency unregistered so the rest of the system has no conditional
/// branches: multiple-choice assessments work normally, and coding questions report a clear
/// "execution unavailable" instead of throwing a resolution error at request time.
/// </summary>
public sealed class DisabledCodeExecutor : ICodeExecutor
{
    private const string Reason =
        "Code execution is not configured on this environment. Set CodeExecution:Provider and " +
        "CodeExecution:BaseUrl to enable coding questions.";

    public bool IsEnabled => false;

    public IReadOnlyList<CodeLanguage> SupportedLanguages => Array.Empty<CodeLanguage>();

    public Task<CodeExecutionResult> ExecuteAsync(CodeExecutionRequest request, CancellationToken ct)
        => Task.FromResult(CodeExecutionResult.Unavailable(Reason));
}
