namespace CareerPlatform.Api.Configuration;

/// <summary>
/// Strongly-typed binding for the <c>Observability</c> configuration section (Req 15.1).
/// Consumed by the OpenTelemetry registration (task 5.3).
/// </summary>
public sealed class ObservabilityOptions
{
    public const string Section = "Observability";

    /// <summary>Service name reported to traces/metrics resources.</summary>
    public string ServiceName { get; init; } = "CareerPlatform.Api";

    /// <summary>OTLP collector endpoint. Empty disables the OTLP exporter.</summary>
    public string? OtlpEndpoint { get; init; }

    /// <summary>Whether distributed tracing is enabled.</summary>
    public bool TracingEnabled { get; init; }

    /// <summary>Whether metrics collection is enabled.</summary>
    public bool MetricsEnabled { get; init; }
}
