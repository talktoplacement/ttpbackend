using CareerPlatform.Api.Configuration;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CareerPlatform.Api.Infrastructure.Observability;

/// <summary>
/// Wires OpenTelemetry metrics and distributed tracing with fail-fast configuration validation
/// (Req 13.3, 13.5).
/// </summary>
/// <remarks>
/// Behavior is driven by the bound <see cref="ObservabilityOptions"/>:
/// <list type="bullet">
///   <item>Both tracing and metrics disabled → no-op: no instrumentation or exporters are
///   registered and startup proceeds.</item>
///   <item>Either enabled → the OTLP exporter endpoint is validated. An empty or malformed
///   <see cref="ObservabilityOptions.OtlpEndpoint"/> is an identifying startup error (Req 13.5),
///   thrown here so misconfiguration halts the app before it serves traffic.</item>
/// </list>
/// ASP.NET Core, <c>HttpClient</c>, and EF Core instrumentation are attached to the enabled
/// signals, exporting over OTLP.
/// </remarks>
public static class ObservabilityRegistration
{
    /// <summary>
    /// Registers OpenTelemetry tracing/metrics per <see cref="ObservabilityOptions"/>, or does
    /// nothing when both signals are disabled. Throws <see cref="InvalidOperationException"/> when
    /// observability is enabled but the OTLP endpoint is missing or invalid (Req 13.5).
    /// </summary>
    public static IServiceCollection AddObservability(
        this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = configuration.GetSection(ObservabilityOptions.Section).Get<ObservabilityOptions>()
            ?? new ObservabilityOptions();

        // Both signals off: register nothing (no exporters) and do not throw.
        if (!options.TracingEnabled && !options.MetricsEnabled)
        {
            return services;
        }

        // Fail-fast: an enabled signal requires a valid OTLP exporter endpoint (Req 13.5).
        var endpoint = ValidateExporterEndpoint(options);

        var serviceName = string.IsNullOrWhiteSpace(options.ServiceName)
            ? "CareerPlatform.Api"
            : options.ServiceName;

        var resourceBuilder = ResourceBuilder.CreateDefault().AddService(serviceName);

        var otel = services.AddOpenTelemetry();

        if (options.TracingEnabled)
        {
            otel.WithTracing(tracing => tracing
                .SetResourceBuilder(resourceBuilder)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddOtlpExporter(exporter => exporter.Endpoint = endpoint));
        }

        if (options.MetricsEnabled)
        {
            otel.WithMetrics(metrics => metrics
                .SetResourceBuilder(resourceBuilder)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(exporter => exporter.Endpoint = endpoint));
        }

        return services;
    }

    /// <summary>
    /// Validates that an enabled observability configuration supplies a well-formed absolute OTLP
    /// endpoint, throwing an identifying <see cref="InvalidOperationException"/> otherwise.
    /// </summary>
    private static Uri ValidateExporterEndpoint(ObservabilityOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.OtlpEndpoint))
        {
            throw new InvalidOperationException(
                "Observability misconfiguration: 'Observability:TracingEnabled' and/or " +
                "'Observability:MetricsEnabled' is true but 'Observability:OtlpEndpoint' is empty. " +
                "Provide a valid OTLP exporter endpoint or disable tracing and metrics.");
        }

        if (!Uri.TryCreate(options.OtlpEndpoint, UriKind.Absolute, out var endpoint))
        {
            throw new InvalidOperationException(
                $"Observability misconfiguration: 'Observability:OtlpEndpoint' value " +
                $"'{options.OtlpEndpoint}' is not a valid absolute URI.");
        }

        return endpoint;
    }
}
