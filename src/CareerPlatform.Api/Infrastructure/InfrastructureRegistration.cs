using CareerPlatform.Api.Infrastructure.Caching;
using CareerPlatform.Api.Infrastructure.Email;
using CareerPlatform.Api.Infrastructure.Security;
using CareerPlatform.Api.Infrastructure.Messaging;
using CareerPlatform.Api.Infrastructure.Payments;
using CareerPlatform.Api.Infrastructure.Search;
using CareerPlatform.Api.Infrastructure.Storage;
using StackExchange.Redis;

namespace CareerPlatform.Api.Infrastructure;

/// <summary>
/// Registers exactly one concrete adapter per infrastructure abstraction (Req 17.2). Swapping
/// an implementation (e.g. a second <see cref="IPaymentGateway"/>) is a registration-only
/// change here and requires no slice edits (Req 17.5).
/// </summary>
public static class InfrastructureRegistration
{
    /// <summary>
    /// Wires the default infrastructure adapters and the shared Redis connection multiplexer.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Shared Redis connection multiplexer. Registered via a factory so the connection is
        // established lazily on first resolution — the host STARTS even when Redis is
        // unreachable. AbortOnConnectFail=false means Connect() never throws on an unreachable
        // endpoint; it returns a multiplexer that keeps retrying in the background. This is
        // critical so the integration-test host does not crash without a live Redis (Req 17.2).
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var connectionString = configuration.GetConnectionString("Redis");

            ConfigurationOptions config;
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                // No Redis configured: register a placeholder that never connects eagerly and
                // never aborts on failure, so the host still starts.
                config = new ConfigurationOptions { AbortOnConnectFail = false };
                config.EndPoints.Add("localhost", 6379);
            }
            else
            {
                config = ConfigurationOptions.Parse(connectionString);
                config.AbortOnConnectFail = false;
            }

            return ConnectionMultiplexer.Connect(config);
        });

        // Exactly one concrete adapter per abstraction (Req 17.2).
        services.AddScoped<ICacheService, RedisCacheService>();
        // File storage adapter selected at startup by `Storage:Provider`. The R2/S3 branch
        // requires bucket + endpoint + credentials to be present — the R2FileStorage ctor
        // validates them and throws early if misconfigured, so the app fails to start rather
        // than silently falling back to the local disk in production.
        var storageProvider =
            configuration.GetValue<string>($"{Configuration.StorageOptions.Section}:Provider")
            ?? "Local";
        if (storageProvider.Equals("R2", StringComparison.OrdinalIgnoreCase) ||
            storageProvider.Equals("S3", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IFileStorage, R2FileStorage>();
        }
        else
        {
            services.AddScoped<IFileStorage, LocalFileStorage>();
        }
        services.AddScoped<ISearchService, SearchService>();
        // All outbound email (OTP, password reset, admin/promotional) goes through Brevo.
        // The "brevo" named HttpClient is registered below and shared with BrevoOtpEmailSender.
        services.AddScoped<IEmailSender, BrevoEmailSender>();
        services.AddScoped<IMessagePublisher, LoggingMessagePublisher>();
        services.AddScoped<IPaymentGateway, RazorpayPaymentGateway>();

        // Cluster-wide mutual exclusion for periodic jobs, backed by Postgres advisory locks.
        // Singleton: it holds no per-request state and opens a dedicated connection per acquisition.
        services.AddSingleton<Concurrency.IDistributedLock, Concurrency.PostgresAdvisoryLock>();

        // --- Code execution sandbox (coding assessments) ---
        // Exactly one adapter is registered, chosen by CodeExecution:Provider. When no sandbox is
        // configured the null-object executor is registered instead of nothing, so multiple-choice
        // assessments keep working and coding questions report a clear "unavailable" message rather
        // than failing to resolve a dependency at request time.
        var codeExecutionProvider = configuration
            .GetValue<Configuration.CodeExecutionProvider?>(
                $"{Configuration.CodeExecutionOptions.Section}:Provider")
            ?? Configuration.CodeExecutionProvider.Disabled;

        if (codeExecutionProvider == Configuration.CodeExecutionProvider.Piston)
        {
            services.AddHttpClient(CodeExecution.PistonCodeExecutor.HttpClientName, (sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<Configuration.CodeExecutionOptions>>().Value;
                if (!string.IsNullOrWhiteSpace(options.BaseUrl))
                {
                    client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
                }
                client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.RequestTimeoutSeconds));
            });
            services.AddSingleton<CodeExecution.ICodeExecutor, CodeExecution.PistonCodeExecutor>();
        }
        else
        {
            services.AddSingleton<CodeExecution.ICodeExecutor, CodeExecution.DisabledCodeExecutor>();
        }

        // --- Custom-auth pipeline ---
        // BCrypt password hasher (work factor 12) — singleton, no per-request state.
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();

        // OTP generator/hasher (HMAC-SHA256, cryptographic RNG, operator-provided hash key).
        services.AddSingleton<IOtpService, HmacOtpService>();

        // JWT issuer that emits tokens accepted by the existing JwtBearer validation config
        // (same secret, same issuer/audience/role-claim). Singleton — reads immutable options.
        services.AddSingleton<IJwtIssuer, JwtIssuer>();

        // Session-cookie writer. Singleton — stateless, reads the validated cookie policy.
        services.AddSingleton<IAuthSessionCookie, AuthSessionCookie>();

        // Brevo transactional-email adapter for OTP delivery. Registered via IHttpClientFactory
        // so timeouts, DNS, and connection pooling are managed centrally.
        services.AddHttpClient("brevo");
        services.AddScoped<IOtpEmailSender, BrevoOtpEmailSender>();


        return services;
    }
}
