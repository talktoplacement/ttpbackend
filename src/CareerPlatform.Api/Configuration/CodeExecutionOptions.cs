using System.ComponentModel.DataAnnotations;

namespace CareerPlatform.Api.Configuration;

/// <summary>Selectable code-execution backends.</summary>
public enum CodeExecutionProvider
{
    /// <summary>No sandbox. Coding questions cannot be graded; MCQ grading is unaffected.</summary>
    Disabled = 0,

    /// <summary>
    /// Piston — an open-source, self-hostable sandbox with a simple HTTP API and broad language
    /// support. Point <see cref="CodeExecutionOptions.BaseUrl"/> at your own instance in production;
    /// the public demo API is heavily rate-limited and unsuitable for real traffic.
    /// </summary>
    Piston = 1,
}

/// <summary>
/// Bound from the <c>CodeExecution</c> section. Chooses and configures the sandbox that runs student
/// submissions for coding questions.
/// </summary>
public sealed class CodeExecutionOptions
{
    public const string Section = "CodeExecution";

    /// <summary>Which backend to use. Defaults to <see cref="CodeExecutionProvider.Disabled"/>.</summary>
    [EnumDataType(typeof(CodeExecutionProvider))]
    public CodeExecutionProvider Provider { get; set; } = CodeExecutionProvider.Disabled;

    /// <summary>Base URL of the execution service (required unless the provider is Disabled).</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>HTTP timeout for one execution call. Kept above the per-run limit.</summary>
    [Range(1, 120)]
    public int RequestTimeoutSeconds { get; set; } = 20;

    /// <summary>
    /// Hard ceiling on any single run, regardless of what a question asks for. Prevents a
    /// misconfigured question from tying up sandbox capacity.
    /// </summary>
    [Range(100, 60_000)]
    public int MaxTimeLimitMs { get; set; } = 10_000;

    /// <summary>
    /// Largest submission the API will accept, in characters. Bounds both the autosave payload and
    /// the sandbox call so a pathological paste cannot exhaust the request pipeline.
    /// </summary>
    [Range(1_000, 1_000_000)]
    public int MaxSourceCodeLength { get; set; } = 64_000;

    /// <summary>Largest stdin payload accepted for an ad-hoc run, in characters.</summary>
    [Range(0, 100_000)]
    public int MaxStdinLength { get; set; } = 10_000;

    /// <summary>
    /// Wall-clock budget for an ad-hoc "Run" from the code viewer, which has no question to inherit a
    /// limit from. Still clamped by <see cref="MaxTimeLimitMs"/>.
    /// </summary>
    [Range(100, 60_000)]
    public int ScratchpadTimeLimitMs { get; set; } = 5_000;

    /// <summary>
    /// Permitted sandbox runs per user per <see cref="RunWindowSeconds"/>. Bounds compute, not just
    /// request volume — every permitted call starts a sandbox process.
    /// </summary>
    [Range(1, 1_000)]
    public int RunsPerWindow { get; set; } = 20;

    /// <summary>Length of the sandbox rate-limit window, in seconds.</summary>
    [Range(1, 3_600)]
    public int RunWindowSeconds { get; set; } = 60;

    /// <summary>
    /// Languages offered to students, as <c>id|label|runtimeVersion</c> or
    /// <c>id|label|runtimeVersion|runtimeName</c> entries. The fourth part is only needed when the
    /// sandbox's own language name differs from the id you want stored on an answer row — for example
    /// <c>cpp|C++ 17|10.2.0|c++</c>.
    ///
    /// Empty by default: the catalog is operator-owned configuration (see
    /// <c>application.properties</c>), so a language can be added or removed without a code change
    /// and nothing is implied by the binary.
    /// </summary>
    public List<string> Languages { get; set; } = new();
}
