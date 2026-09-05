namespace CareerPlatform.Api.Infrastructure.CodeExecution;

/// <summary>
/// One program run: source code plus the stdin to feed it.
/// </summary>
/// <param name="Language">Language id from <see cref="ICodeExecutor.SupportedLanguages"/>.</param>
/// <param name="SourceCode">Complete program source.</param>
/// <param name="Stdin">Text written to the program's standard input.</param>
/// <param name="TimeLimitMs">Wall-clock budget; the executor must abort beyond it.</param>
public sealed record CodeExecutionRequest(
    string Language,
    string SourceCode,
    string Stdin,
    int TimeLimitMs);

/// <summary>The outcome of a single run.</summary>
/// <param name="Succeeded">
/// True when the program compiled and ran to completion. This says nothing about whether the OUTPUT
/// was correct — comparison against expected output is the grader's job, not the executor's.
/// </param>
/// <param name="Stdout">Captured standard output.</param>
/// <param name="Stderr">Captured standard error (compiler/runtime diagnostics).</param>
/// <param name="TimedOut">True when the run exceeded its time budget.</param>
/// <param name="FailureReason">Set when the run could not be performed at all.</param>
public sealed record CodeExecutionResult(
    bool Succeeded,
    string Stdout,
    string Stderr,
    bool TimedOut,
    string? FailureReason)
{
    public static CodeExecutionResult Unavailable(string reason) =>
        new(false, string.Empty, string.Empty, false, reason);
}

/// <summary>A language the current executor can run.</summary>
/// <param name="Id">Stable identifier stored on an answer, e.g. <c>python</c>.</param>
/// <param name="Label">Display name, e.g. <c>Python 3</c>.</param>
public sealed record CodeLanguage(string Id, string Label);

/// <summary>
/// Runs untrusted student code against test inputs.
///
/// Behind an interface on purpose: executing arbitrary submitted code safely requires a sandbox, and
/// which sandbox is an operational decision (a hosted Piston instance, a self-hosted one, a
/// commercial judge). Callers depend only on this contract, so swapping providers is a registration
/// change — no grading or endpoint code moves.
///
/// Implementations MUST NOT throw for ordinary failures (bad code, timeout, provider down); they
/// return an unsuccessful <see cref="CodeExecutionResult"/> so one broken submission can never take
/// down a request.
/// </summary>
public interface ICodeExecutor
{
    /// <summary>Whether a real sandbox is configured. False means coding questions cannot be graded.</summary>
    bool IsEnabled { get; }

    /// <summary>Languages the student may choose from.</summary>
    IReadOnlyList<CodeLanguage> SupportedLanguages { get; }

    /// <summary>Runs one program. Never throws for an ordinary failure.</summary>
    Task<CodeExecutionResult> ExecuteAsync(CodeExecutionRequest request, CancellationToken ct);
}
