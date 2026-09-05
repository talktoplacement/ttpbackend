using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CareerPlatform.Api.Configuration;

namespace CareerPlatform.Api.Infrastructure.CodeExecution;

/// <summary>
/// <see cref="ICodeExecutor"/> backed by Piston (<c>POST {BaseUrl}/api/v2/execute</c>).
///
/// Piston runs each submission in a throwaway sandbox with its own time and output limits, which is
/// what makes executing untrusted student code acceptable. Self-host it in production — the public
/// demo instance is rate-limited and offers no availability guarantee.
///
/// Every failure mode (non-2xx, network error, malformed body, cancellation) is converted into an
/// unsuccessful result rather than an exception, so a bad submission or a sandbox outage degrades
/// that one question instead of failing the request.
/// </summary>
public sealed class PistonCodeExecutor : ICodeExecutor
{
    /// <summary>Named HttpClient registered in <c>InfrastructureRegistration</c>.</summary>
    public const string HttpClientName = "code-execution";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly CodeExecutionOptions _options;
    private readonly ILogger<PistonCodeExecutor> _logger;
    private readonly Dictionary<string, LanguageRuntime> _runtimes;

    public PistonCodeExecutor(
        IHttpClientFactory httpClientFactory,
        IOptions<CodeExecutionOptions> options,
        ILogger<PistonCodeExecutor> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
        _runtimes = ParseLanguages(_options.Languages);
    }

    public bool IsEnabled => !string.IsNullOrWhiteSpace(_options.BaseUrl) && _runtimes.Count > 0;

    public IReadOnlyList<CodeLanguage> SupportedLanguages =>
        _runtimes.Values.Select(r => new CodeLanguage(r.Id, r.Label)).ToList();

    public async Task<CodeExecutionResult> ExecuteAsync(
        CodeExecutionRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!IsEnabled)
        {
            return CodeExecutionResult.Unavailable("Code execution is not configured.");
        }
        if (!_runtimes.TryGetValue(request.Language, out var runtime))
        {
            return CodeExecutionResult.Unavailable($"Unsupported language '{request.Language}'.");
        }

        // Clamp to the operator ceiling so one question cannot monopolise sandbox capacity.
        var timeLimitMs = Math.Clamp(request.TimeLimitMs, 100, _options.MaxTimeLimitMs);

        var payload = new PistonExecuteRequest
        {
            Language = runtime.PistonLanguage,
            Version = runtime.Version,
            Files = [new PistonFile { Content = request.SourceCode }],
            Stdin = request.Stdin,
            RunTimeoutMs = timeLimitMs,
            CompileTimeoutMs = timeLimitMs,
        };

        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            client.BaseAddress ??= new Uri(_options.BaseUrl, UriKind.Absolute);

            using var response = await client.PostAsJsonAsync("api/v2/execute", payload, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Code execution provider returned {Status} for language {Language}.",
                    (int)response.StatusCode, request.Language);
                return CodeExecutionResult.Unavailable(
                    $"The code execution service responded with status {(int)response.StatusCode}.");
            }

            var body = await response.Content.ReadFromJsonAsync<PistonExecuteResponse>(ct);
            if (body is null)
            {
                return CodeExecutionResult.Unavailable(
                    "The code execution service returned an empty response.");
            }

            // Piston reports compile and run stages separately; a compile error means no run output.
            if (body.Compile is { Code: not 0 } compile)
            {
                return new CodeExecutionResult(
                    Succeeded: false,
                    Stdout: string.Empty,
                    Stderr: Truncate(compile.Stderr ?? compile.Output ?? "Compilation failed."),
                    TimedOut: false,
                    FailureReason: "Compilation failed.");
            }

            var run = body.Run;
            if (run is null)
            {
                return CodeExecutionResult.Unavailable(
                    "The code execution service returned no run stage.");
            }

            // Piston signals a timeout kill via SIGKILL on the run stage.
            var timedOut = string.Equals(run.Signal, "SIGKILL", StringComparison.OrdinalIgnoreCase);

            return new CodeExecutionResult(
                Succeeded: !timedOut && run.Code == 0,
                Stdout: Truncate(run.Stdout ?? string.Empty),
                Stderr: Truncate(run.Stderr ?? string.Empty),
                TimedOut: timedOut,
                FailureReason: timedOut
                    ? $"Execution exceeded the {timeLimitMs} ms time limit."
                    : run.Code == 0 ? null : "The program exited with a non-zero status.");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw; // caller is going away; not an execution failure
        }
        catch (Exception ex)
        {
            // Network failure, DNS, TLS, malformed JSON — degrade this question only.
            _logger.LogWarning(ex, "Code execution request failed for language {Language}.", request.Language);
            return CodeExecutionResult.Unavailable("The code execution service is unavailable.");
        }
    }

    /// <summary>Caps captured streams so a runaway print loop cannot bloat a response or the DB.</summary>
    private static string Truncate(string value, int max = 10_000) =>
        value.Length <= max ? value : value[..max] + "\n… output truncated …";

    /// <summary>
    /// Parses the configured language entries: <c>id|label|version</c>, or
    /// <c>id|label|version|runtimeName</c> when the sandbox's own language name differs from the id
    /// we expose.
    ///
    /// The optional fourth part matters for scaling the catalog: Piston names some runtimes
    /// differently from a URL- and storage-safe public id (for example the id <c>cpp</c> against
    /// Piston's <c>c++</c>, or <c>node</c> against <c>javascript</c>). Without it, adding such a
    /// language would mean either storing an awkward id on every answer row or editing this class.
    /// </summary>
    private static Dictionary<string, LanguageRuntime> ParseLanguages(IEnumerable<string> configured)
    {
        var map = new Dictionary<string, LanguageRuntime>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in configured ?? Enumerable.Empty<string>())
        {
            var parts = (raw ?? string.Empty).Split('|', StringSplitOptions.TrimEntries);
            if (parts.Length < 3 || parts[0].Length == 0 || parts[2].Length == 0) continue;

            var id = parts[0];
            var label = parts[1].Length > 0 ? parts[1] : id;
            var version = parts[2];
            // Defaults to the id, so the common case stays a three-part entry.
            var runtimeName = parts.Length > 3 && parts[3].Length > 0 ? parts[3] : id;

            map[id] = new LanguageRuntime(id, label, runtimeName, version);
        }
        return map;
    }

    private sealed record LanguageRuntime(string Id, string Label, string PistonLanguage, string Version);

    // ── Piston wire contracts ────────────────────────────────────────────────

    private sealed class PistonExecuteRequest
    {
        [JsonPropertyName("language")] public string Language { get; set; } = string.Empty;
        [JsonPropertyName("version")] public string Version { get; set; } = string.Empty;
        [JsonPropertyName("files")] public List<PistonFile> Files { get; set; } = new();
        [JsonPropertyName("stdin")] public string Stdin { get; set; } = string.Empty;
        [JsonPropertyName("run_timeout")] public int RunTimeoutMs { get; set; }
        [JsonPropertyName("compile_timeout")] public int CompileTimeoutMs { get; set; }
    }

    private sealed class PistonFile
    {
        [JsonPropertyName("content")] public string Content { get; set; } = string.Empty;
    }

    private sealed class PistonExecuteResponse
    {
        [JsonPropertyName("run")] public PistonStage? Run { get; set; }
        [JsonPropertyName("compile")] public PistonStage? Compile { get; set; }
    }

    private sealed class PistonStage
    {
        [JsonPropertyName("stdout")] public string? Stdout { get; set; }
        [JsonPropertyName("stderr")] public string? Stderr { get; set; }
        [JsonPropertyName("output")] public string? Output { get; set; }
        [JsonPropertyName("code")] public int? Code { get; set; }
        [JsonPropertyName("signal")] public string? Signal { get; set; }
    }
}
