using CareerPlatform.Api.Configuration;
using CareerPlatform.Api.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CareerPlatform.Api.Health;

/// <summary>
/// Registers and maps liveness/readiness health checks (Req 16).
///
/// Liveness (<c>/health/live</c>) runs no dependency checks and answers within the liveness
/// budget so an orchestrator can tell the process is up even when its dependencies are down
/// (Req 16.1, 16.4). Readiness (<c>/health/ready</c>) aggregates the dependency checks — Postgres,
/// Redis, storage, and external services — each tagged <see cref="ReadyTag"/> with a per-dependency
/// timeout so a single slow dependency cannot stall the endpoint (Req 16.2, 16.3, 16.5).
/// </summary>
public static class HealthChecksRegistration
{
    /// <summary>Tag applied to every dependency check so readiness can select them (Req 16.2).</summary>
    public const string ReadyTag = "ready";

    /// <summary>
    /// Registers the readiness dependency checks. The Npgsql and Redis checks resolve their
    /// connection strings from configuration; the storage and external-services checks are
    /// lightweight in-process probes. Every dependency check is tagged <see cref="ReadyTag"/> and
    /// given a per-dependency timeout so readiness aggregates per-dependency status without a slow
    /// dependency stalling the whole response (Req 16.2, 16.3, 16.5).
    /// </summary>
    public static IServiceCollection AddAppHealthChecks(
        this IServiceCollection services, IConfiguration configuration)
    {
        var health = configuration.GetSection(HealthOptions.Section).Get<HealthOptions>()
            ?? new HealthOptions();
        var dependencyTimeout = TimeSpan.FromSeconds(Math.Max(1, health.DependencyTimeoutSeconds));

        // The Npgsql/Redis check builders reject a null/empty connection string at registration
        // time, which would halt host startup when a dependency is unconfigured (e.g. in the test
        // host). Fall back to a syntactically-valid, fast-failing placeholder so registration
        // succeeds and the dependency instead surfaces as Unhealthy at check time (Req 16.2, 16.3).
        var postgresConnection = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(postgresConnection))
        {
            postgresConnection =
                "Host=localhost;Database=unconfigured;Username=unconfigured;Password=unconfigured;" +
                "Timeout=2;Command Timeout=2";
        }

        // Redis is an OPTIONAL dependency in this deployment: operators disable it by leaving the
        // connection string empty or setting it to "disabled". Registering a check against a
        // deliberately-absent dependency would make readiness permanently Unhealthy and mask real
        // outages, so the check is only added when Redis is actually configured.
        var redisConnection = configuration.GetConnectionString("Redis");
        var redisEnabled =
            !string.IsNullOrWhiteSpace(redisConnection)
            && !redisConnection.Equals("disabled", StringComparison.OrdinalIgnoreCase);

        var builder = services.AddHealthChecks();

        if (redisEnabled)
        {
            builder.AddRedis(
                redisConnectionString: redisConnection!,
                name: "redis",
                failureStatus: HealthStatus.Unhealthy,
                tags: [ReadyTag],
                timeout: dependencyTimeout);
        }

        builder
            // PostgreSQL dependency (Req 16.2). Unhealthy — not a hang — when unreachable.
            .AddNpgSql(
                connectionString: postgresConnection,
                name: "postgres",
                failureStatus: HealthStatus.Unhealthy,
                tags: [ReadyTag],
                timeout: dependencyTimeout)
            // Storage dependency — real write/read/delete round-trip (Req 16.2).
            .AddCheck<StorageHealthCheck>(
                name: "storage",
                failureStatus: HealthStatus.Unhealthy,
                tags: [ReadyTag],
                timeout: dependencyTimeout)
            // External integrations — configuration completeness (Req 16.2).
            .AddCheck<ExternalServicesHealthCheck>(
                name: "external-services",
                failureStatus: HealthStatus.Unhealthy,
                tags: [ReadyTag],
                timeout: dependencyTimeout);

        return services;
    }

    /// <summary>
    /// Maps the two health endpoints. Both are anonymous so they answer even under the fail-closed
    /// fallback authorization policy (Req 16.5): <c>/health/live</c> runs no checks and returns
    /// quickly (Req 16.1, 16.4); <c>/health/ready</c> runs only the <see cref="ReadyTag"/>-tagged
    /// dependency checks and reports each dependency's status (Req 16.2, 16.5).
    /// </summary>
    public static WebApplication MapAppHealthChecks(this WebApplication app)
    {
        // Liveness: no dependency checks — the predicate selects zero registrations, so the endpoint
        // reports Healthy purely on the host being able to serve the request (Req 16.1, 16.4).
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
        }).AllowAnonymous();

        // Readiness: only the dependency checks tagged "ready" (Req 16.2, 16.5).
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(ReadyTag),
        }).AllowAnonymous();

        return app;
    }

    /// <summary>
    /// Readiness probe for the file-storage dependency. Performs a real write → read → delete
    /// round-trip against the configured <see cref="IFileStorage"/> adapter (local disk or
    /// R2/S3), so a broken bucket, bad credentials, or an unwritable volume surfaces as Unhealthy
    /// instead of being masked.
    ///
    /// The probe always uses the same fixed key, so repeated readiness polls overwrite one object
    /// rather than accumulating garbage.
    /// </summary>
    private sealed class StorageHealthCheck : IHealthCheck
    {
        /// <summary>Fixed probe key — reused every poll so no garbage accumulates.</summary>
        private const string ProbeKey = "_health/readiness-probe";

        private static readonly byte[] ProbePayload = "ok"u8.ToArray();

        private readonly IFileStorage _storage;

        public StorageHealthCheck(IFileStorage storage) => _storage = storage;

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                using var input = new MemoryStream(ProbePayload, writable: false);
                var key = await _storage.SaveAsync(input, ProbeKey, cancellationToken);

                await using (var readBack = await _storage.OpenAsync(key, cancellationToken))
                {
                    using var buffer = new MemoryStream();
                    await readBack.CopyToAsync(buffer, cancellationToken);
                    if (buffer.Length != ProbePayload.Length)
                    {
                        return HealthCheckResult.Unhealthy(
                            "Storage round-trip returned unexpected content length.");
                    }
                }

                await _storage.DeleteAsync(key, cancellationToken);
                return HealthCheckResult.Healthy("Storage write/read/delete round-trip succeeded.");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Storage round-trip failed.", ex);
            }
        }
    }

    /// <summary>
    /// Readiness probe for the outbound third-party integrations (Brevo email, Razorpay payments).
    ///
    /// This deliberately validates that each integration is <em>configured</em> rather than issuing
    /// live HTTP calls: hammering a payment gateway and a mail provider on every readiness poll
    /// would burn rate limit/quota and make readiness depend on third-party latency. A missing
    /// credential is reported as <see cref="HealthStatus.Degraded"/> — the API still serves traffic,
    /// but the operator gets a truthful signal that email or payments will fail.
    /// </summary>
    private sealed class ExternalServicesHealthCheck : IHealthCheck
    {
        private readonly BrevoOptions _brevo;
        private readonly RazorpayOptions _razorpay;

        public ExternalServicesHealthCheck(
            IOptions<BrevoOptions> brevo, IOptions<RazorpayOptions> razorpay)
        {
            _brevo = brevo.Value;
            _razorpay = razorpay.Value;
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var unconfigured = new List<string>(2);

            if (string.IsNullOrWhiteSpace(_brevo.ApiKey))
            {
                unconfigured.Add("Brevo (email/OTP delivery)");
            }
            if (string.IsNullOrWhiteSpace(_razorpay.KeyId)
                || string.IsNullOrWhiteSpace(_razorpay.KeySecret))
            {
                unconfigured.Add("Razorpay (payments)");
            }

            return Task.FromResult(unconfigured.Count == 0
                ? HealthCheckResult.Healthy("Brevo and Razorpay credentials are configured.")
                : HealthCheckResult.Degraded(
                    "Unconfigured external integration(s): " + string.Join(", ", unconfigured)));
        }
    }
}
