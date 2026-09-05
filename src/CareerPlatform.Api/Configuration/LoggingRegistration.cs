using Serilog;
using Serilog.Formatting.Compact;

namespace CareerPlatform.Api.Configuration;

/// <summary>
/// Configures Serilog as the application's structured-logging sink (Req 13.1, 13.4).
/// </summary>
/// <remarks>
/// Emits compact JSON to the console via <see cref="CompactJsonFormatter"/>, which renders the
/// inherent named properties (<c>@t</c> timestamp, <c>@l</c> level, <c>@mt</c> message template,
/// and <c>SourceContext</c> source). <c>Enrich.FromLogContext()</c> surfaces the
/// <c>CorrelationId</c> pushed by <see cref="Middleware.CorrelationIdMiddleware"/> on every entry
/// (Req 13.4). Serilog is only the sink here — request/response summary logging remains the job of
/// <see cref="Middleware.RequestLoggingMiddleware"/>, so nothing is double-logged.
/// </remarks>
public static class LoggingRegistration
{
    /// <summary>
    /// Replaces the default logging providers with Serilog, wiring the compact-JSON console sink,
    /// log-context enrichment, and any <c>Serilog</c> configuration section overrides.
    /// </summary>
    public static WebApplicationBuilder AddStructuredLogging(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Host.UseSerilog((context, services, loggerConfiguration) =>
            loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", context.HostingEnvironment.ApplicationName)
                .WriteTo.Console(new CompactJsonFormatter()));

        return builder;
    }
}
