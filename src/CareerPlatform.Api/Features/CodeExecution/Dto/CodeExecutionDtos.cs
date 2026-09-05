namespace CareerPlatform.Api.Features.CodeExecution.Dto;

/// <summary>A language the sandbox can run, as offered to the client.</summary>
public sealed record RunnableLanguageResponse(string Id, string Label);

/// <summary>
/// What the client needs to decide whether to offer a Run action at all.
///
/// <see cref="Enabled"/> is false when no sandbox is configured. The UI hides Run rather than
/// presenting a button that always errors — the previous viewer shipped a decorative Run that only
/// ran a <c>setTimeout</c>, which is worse than no button.
/// </summary>
public sealed record RunnableLanguageCatalogResponse(
    bool Enabled,
    IReadOnlyList<RunnableLanguageResponse> Languages);

/// <summary>Body for an ad-hoc run from the code viewer.</summary>
public sealed record CodeRunRequest(string Language, string SourceCode, string? Stdin);

/// <summary>
/// Result of an ad-hoc run.
///
/// Deliberately reports failure in-band rather than as an HTTP error: a compile error or a
/// non-zero exit is a normal outcome the user needs to read, not a fault of the request. Only a
/// genuinely bad request (unknown language, oversized payload) is a 4xx.
/// </summary>
public sealed record CodeRunResponse(
    bool ExecutionAvailable,
    bool Succeeded,
    string Stdout,
    string Stderr,
    bool TimedOut,
    string? FailureReason);
