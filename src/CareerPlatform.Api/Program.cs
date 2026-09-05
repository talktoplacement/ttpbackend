using System.Security.Claims;
using System.Threading.RateLimiting;
using CareerPlatform.Api.BackgroundJobs;
using CareerPlatform.Api.Common;
using CareerPlatform.Api.Features.Assessments;
using CareerPlatform.Api.Features.Auth;
using CareerPlatform.Api.Features.Broadcasts;
using CareerPlatform.Api.Features.Certificates;
using CareerPlatform.Api.Features.CodeExecution;
using CareerPlatform.Api.Features.AdminLedger;
using CareerPlatform.Api.Features.Cms;
using CareerPlatform.Api.Features.Content;
using CareerPlatform.Api.Features.Coupons;
using CareerPlatform.Api.Features.CourseCategories;
using CareerPlatform.Api.Features.CourseLessons;
using CareerPlatform.Api.Features.Courses;
using CareerPlatform.Api.Features.Dashboard;
using CareerPlatform.Api.Features.Interviews;
using CareerPlatform.Api.Features.Learning;
using CareerPlatform.Api.Features.LearningPaths;
using CareerPlatform.Api.Features.Meetings;
using CareerPlatform.Api.Features.MentorAssignments;
using CareerPlatform.Api.Features.MentorPortal;
using CareerPlatform.Api.Features.Mentorship;
using CareerPlatform.Api.Features.Notifications;
using CareerPlatform.Api.Features.Offers;
using CareerPlatform.Api.Features.PlacementCompanies;
using CareerPlatform.Api.Features.PlacementPlans;
using CareerPlatform.Api.Features.MentorshipPlans;
using CareerPlatform.Api.Features.PlacementReadiness;
using CareerPlatform.Api.Features.PlacementRoles;
using CareerPlatform.Api.Features.StudentProfile;
using CareerPlatform.Api.Features.Payments;
using CareerPlatform.Api.Features.Posts;
using CareerPlatform.Api.Features.Practice;
using CareerPlatform.Api.Features.PracticeBanks;
using CareerPlatform.Api.Features.Products;
using CareerPlatform.Api.Features.Resumes;
using CareerPlatform.Api.Features.Reviews;
using CareerPlatform.Api.Features.Settings;
using CareerPlatform.Api.Features.Skills;
using CareerPlatform.Api.Features.SubscriptionPlans;
using CareerPlatform.Api.Features.Support;
using CareerPlatform.Api.Features.Users;
using CareerPlatform.Api.Features.SubscriptionPlans.Lifecycle;
using CareerPlatform.Api.Configuration;
using CareerPlatform.Api.Configuration.Properties;
using CareerPlatform.Api.Health;
using CareerPlatform.Api.Infrastructure;
using CareerPlatform.Api.Infrastructure.Observability;
using CareerPlatform.Api.Infrastructure.Persistence;
using CareerPlatform.Api.Infrastructure.Persistence.Seed;
using CareerPlatform.Api.Middleware;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

// Load .env (application.properties-style) into process environment before the ASP.NET Core
// configuration provider chain reads env vars. Real environment variables always win.
EnvFileLoader.Load();

var builder = WebApplication.CreateBuilder(args);

// Operator-owned settings file (Java-style key=value). Registered AFTER appsettings*.json so it
// overrides them, but BEFORE environment variables — which the default builder adds last — so a real
// env var still wins. `reloadOnChange` is what lets an operator change a subscription price on a
// running instance: the pricing reconciler subscribes to the change and updates the stored catalog,
// and Razorpay then charges the new amount on the next checkout.
var operatorPropertiesPath = builder.Configuration.AddOperatorPropertiesFile("application.properties");

// Structured logging: Serilog compact-JSON console sink enriched with the correlation id pushed by
// CorrelationIdMiddleware. Serilog is the sink; RequestLoggingMiddleware still owns request summaries
// (Req 13.1, 13.4).
builder.AddStructuredLogging();

// Anti-DoS: clamp the maximum request body size at the server (Kestrel) level so oversized
// payloads are rejected before they are buffered. Configurable via RequestLimits:MaxBodyBytes
// (default 10 MB); individual endpoints (e.g. PDF upload) may raise/lower their own limit.
var maxRequestBodyBytes = builder.Configuration.GetValue<long?>("RequestLimits:MaxBodyBytes")
    ?? 10L * 1024 * 1024;
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = maxRequestBodyBytes;
});
// Honor X-Forwarded-Proto/For from a TLS-terminating proxy/ingress so HTTPS redirection and
// scheme-aware logic see the original client scheme rather than the internal HTTP hop.
builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
        | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    // Trust the immediate proxy chain; clearing these lets container/orchestrator proxies work.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// MVC controllers (new architecture — docs/architecture/structure.md). FluentValidation runs
// via the global FluentValidationActionFilter: on any failure the request short-circuits with
// a 400 ProblemDetails carrying the per-field errors.
builder.Services.AddControllers(options =>
{
    options.Filters.Add<CareerPlatform.Api.Common.FluentValidationActionFilter>();
    // Emits an AdminAuditLog row after every successful state-changing action on an
    // /api/v1/admin/* route. Fail-soft and opt-out-able via [SkipAudit].
    options.Filters.Add<CareerPlatform.Api.Common.AdminAuditActionFilter>();
});

// Feature modules (modular-monolith). Each feature owns a `{Feature}Module.cs` that wires the
// module's DI in a single place. Add new registrations here as features are migrated.
builder.Services.RegisterCourses(builder.Configuration);
builder.Services.RegisterOffers(builder.Configuration);
builder.Services.RegisterPayments(builder.Configuration);
builder.Services.RegisterSettings(builder.Configuration);
builder.Services.RegisterBroadcasts(builder.Configuration);
builder.Services.RegisterDashboard(builder.Configuration);
builder.Services.RegisterLearning(builder.Configuration);
builder.Services.RegisterCertificates(builder.Configuration);
builder.Services.RegisterMeetings(builder.Configuration);
builder.Services.RegisterUsers(builder.Configuration);
builder.Services.RegisterLearningPaths(builder.Configuration);
builder.Services.RegisterNotifications(builder.Configuration);
builder.Services.RegisterPlacementCompanies(builder.Configuration);
builder.Services.RegisterSupport(builder.Configuration);
builder.Services.RegisterAuth(builder.Configuration);
builder.Services.RegisterInterviews(builder.Configuration);
builder.Services.RegisterPractice(builder.Configuration);
builder.Services.RegisterSubscriptionPlans(builder.Configuration);
builder.Services.RegisterAssessments(builder.Configuration);
builder.Services.RegisterCodeExecution(builder.Configuration);
builder.Services.RegisterMentorPortal(builder.Configuration);
builder.Services.RegisterMentorship(builder.Configuration);
builder.Services.RegisterContent(builder.Configuration);
builder.Services.RegisterResumes(builder.Configuration);
builder.Services.RegisterPosts(builder.Configuration);
// Phase 5 admin-surface backfill
builder.Services.RegisterCoupons(builder.Configuration);
builder.Services.RegisterReviews(builder.Configuration);
builder.Services.RegisterCms(builder.Configuration);
builder.Services.RegisterSkills(builder.Configuration);
builder.Services.RegisterCourseCategories(builder.Configuration);
builder.Services.RegisterPlacementRoles(builder.Configuration);
builder.Services.RegisterProducts(builder.Configuration);
builder.Services.RegisterAdminLedger(builder.Configuration);
builder.Services.RegisterCourseLessons(builder.Configuration);
builder.Services.RegisterPracticeBanks(builder.Configuration);
builder.Services.RegisterMentorAssignments(builder.Configuration);
builder.Services.RegisterPlacementPlans(builder.Configuration);
builder.Services.RegisterMentorshipPlans(builder.Configuration);
// Student self-service surface: qualifications, preferences, derived placement readiness
builder.Services.RegisterStudentProfile(builder.Configuration);
builder.Services.RegisterPlacementReadiness(builder.Configuration);

// OpenAPI / Swagger (Req: mobile client contract). Serves the machine-readable spec at
// /swagger/v1/swagger.json and a browsable UI at /swagger. Paths in the document are the
// mobile-canonical /api/v1/kebab-case URLs; the request rewriter below makes those URLs hit the
// existing internal PascalCase handlers.
builder.Services.AddOpenApi();

// Strongly-typed configuration bound to validated options. Secret-bearing options
// (Jwt/Razorpay/Brevo) are validated at startup and fail-fast when required values are
// missing; rate-limit values are clamped to safe ranges (Req 15.1, 15.2, 15.3).
builder.Services.AddHardenedOptions(builder.Configuration);

// OpenTelemetry metrics + tracing (ASP.NET Core, HttpClient, EF Core instrumentation + OTLP
// exporter). Registers a no-op when both signals are disabled and fails startup when enabled with
// an empty/invalid OTLP endpoint (Req 13.3, 13.5).
builder.Services.AddObservability(builder.Configuration);

// Infrastructure adapters (cache, storage, search, email, messaging, payments) behind their
// abstractions, plus the lazily-connected Redis multiplexer. Exactly one concrete adapter per
// abstraction; the Redis registration is fail-soft so the host starts without a live Redis
// (Req 17.2, 17.5).
builder.Services.AddInfrastructure(builder.Configuration);

// Persistence baseline: AppDbContext (Npgsql) + the ICurrentUser accessor. AddDbContext does not
// connect at startup, so the host starts without a reachable database (Req 17.7, 18.1, 19.3).
// Interceptors (tasks 8.3/8.5) attach at the extension point inside AddPersistence.
builder.Services.AddPersistence(builder.Configuration);

// In-process background job processing: a Channel-backed scheduler (singleton) behind
// IBackgroundJobScheduler plus the hosted-service worker that dequeues jobs, runs each in a fresh
// DI scope that outlives the request, retries up to 3 additional times, and logs failures and
// permanent-failure state (Req 23).
builder.Services.AddBackgroundJobs();

// Subscription expiry sweeper: a periodic hosted service that expires past-due subscriptions and
// reverts a student's cached Effective_Plan to "Free" when no active subscription remains. Runs
// each tick in its own DI scope; cadence comes from the validated SubscriptionOptions (Req 11.2,
// 11.3, 11.5).
builder.Services.AddHostedService<SubscriptionExpirySweeper>();

// Mirrors the operator's price list (application.properties) into the SubscriptionPlans table at
// startup and on every subsequent edit of the file, so changing a price needs no code change or
// redeploy. Scoped reconciler + hosted trigger, distributed-locked so only one replica writes.
builder.Services.AddScoped<SubscriptionCatalogReconciler>();
builder.Services.AddHostedService<PricingReconciliationService>();

// FluentValidation: register every validator declared in this assembly. The global
// FluentValidationActionFilter (see AddControllers above) discovers each validator per action
// argument type and invokes it before the controller runs (Req 5.2, 5.7).
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// Liveness/readiness health checks: /health/live (no dependencies) and /health/ready (Postgres,
// Redis, storage, external services with a per-dependency timeout). Mapped after build (Req 16).
builder.Services.AddAppHealthChecks(builder.Configuration);

// RFC 7807 ProblemDetails + the centralized exception handler (from task 4.2). Registered as the
// outermost stage of the pipeline via app.UseExceptionHandler() below (Req 7, 14.1, 14.6).
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Supabase JWT bearer authentication + role-claim normalization, encapsulated in
// AuthenticationRegistration (Req 19, 20). Token-validation parameters are sourced from the
// validated JwtOptions; because JwtOptions.Secret is [Required] with ValidateOnStart, a missing
// secret halts startup before the post-configure runs, so the signing key is always present.
// SupabaseRoleClaimsTransformation normalizes the Supabase role claim to ClaimTypes.Role so
// [Authorize(Roles = "Admin")] / the "Admin" policy resolve correctly.
builder.Services.AddJwtAuthentication(builder.Configuration);

// Fail-closed authorization: the fallback policy requires an authenticated user for every endpoint,
// so anything not explicitly marked AllowAnonymous rejects unauthenticated requests (Req 20.1).
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    // Named policy for administrative endpoints (Req 20.5).
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));

    // Named policy for mentor-only endpoints (mentor dashboard, assigned resumes, etc.).
    options.AddPolicy("Mentor", policy => policy.RequireRole("Mentor"));
});

// API rate limiting for sensitive/administrative endpoints (Req 21). Fixed-window limiter
// partitioned by authenticated subject when present, else client IP, else a constant
// "anonymous" key. Limits are sourced from the validated RateLimitOptions (already clamped to
// their accepted ranges in AddHardenedOptions) and resolved per-request from DI.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy(RateLimitPolicy.Sensitive, httpContext =>
    {
        var rateLimit = httpContext.RequestServices
            .GetRequiredService<IOptions<RateLimitOptions>>().Value;

        var partitionKey =
            httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ =>
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = rateLimit.PermitLimit,
                Window = TimeSpan.FromSeconds(rateLimit.WindowSeconds),
                QueueLimit = 0,
                AutoReplenishment = true,
            });
    });

    // Sandbox execution: a far tighter budget than Sensitive, because each permitted call spawns a
    // sandbox process rather than just touching the database. Always partitioned by authenticated
    // subject — these endpoints require auth, so there is no anonymous bucket to share.
    options.AddPolicy(RateLimitPolicy.CodeExecution, httpContext =>
    {
        var codeExecution = httpContext.RequestServices
            .GetRequiredService<IOptions<CodeExecutionOptions>>().Value;

        var partitionKey =
            httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter($"code:{partitionKey}", _ =>
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = codeExecution.RunsPerWindow,
                Window = TimeSpan.FromSeconds(codeExecution.RunWindowSeconds),
                QueueLimit = 0,
                AutoReplenishment = true,
            });
    });

    // On rejection, emit a Retry-After header. Prefer the lease's RetryAfter metadata (seconds
    // remaining in the window); fall back to the full window length. Always at least 1 second
    // (Req 21.3).
    options.OnRejected = (context, _) =>
    {
        var windowSeconds = context.HttpContext.RequestServices
            .GetRequiredService<IOptions<RateLimitOptions>>().Value.WindowSeconds;

        var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var ra)
            ? RateLimitPolicy.RetryAfterSeconds(ra.TotalSeconds)
            : Math.Max(1, windowSeconds);

        context.HttpContext.Response.Headers.RetryAfter = retryAfter.ToString();
        return ValueTask.CompletedTask;
    };
});


// CORS: allowed origins are sourced from the validated CorsOptions (Cors:AllowedOrigins).
// The single named policy is applied globally below. AllowCredentials is required so that
// browser fetches issued with credentials: "include" can carry the auth cookie/header.
// When AllowedOrigins is empty the policy is registered but matches no origin — effectively
// closed by default. Configure per-environment in appsettings.<Env>.json (Req 3.3, 14.4).
const string CorsPolicyName = "AllowFrontend";
builder.Services.AddCors(options =>
{
    var corsOptions = builder.Configuration
        .GetSection(CorsOptions.Section)
        .Get<CorsOptions>() ?? new CorsOptions();
    var configuredOrigins = corsOptions.AllowedOrigins;

    // Because this policy grants AllowCredentials, a plaintext origin would let the browser send
    // the session cookie over an interceptable channel. Outside Development only https:// origins
    // are trusted; insecure entries are dropped and logged rather than silently honoured.
    var isDevelopment = builder.Environment.IsDevelopment();
    var origins = configuredOrigins
        .Where(origin => !string.IsNullOrWhiteSpace(origin))
        .Where(origin => isDevelopment || CorsOptions.IsSecureOrigin(origin))
        .ToArray();

    // Blank entries are expected (compose passes `${FRONTEND_ORIGIN_PREVIEW:-}`), so they are not
    // reported as rejections — only genuinely unusable values are.
    var rejected = configuredOrigins
        .Where(origin => !string.IsNullOrWhiteSpace(origin))
        .Except(origins, StringComparer.OrdinalIgnoreCase)
        .ToArray();
    if (rejected.Length > 0)
    {
        // Logged via the bootstrap logger — the app must not appear to trust these origins.
        Console.Error.WriteLine(
            $"[CORS] Ignoring {rejected.Length} non-HTTPS allowed-origin(s) outside Development: " +
            string.Join(", ", rejected));
    }

    var usableSuffixes = corsOptions.AllowedOriginSuffixes
        .Where(CorsOptions.IsUsableSuffix)
        .ToArray();
    var unusableSuffixes = corsOptions.AllowedOriginSuffixes
        .Where(s => !string.IsNullOrWhiteSpace(s))
        .Except(usableSuffixes, StringComparer.OrdinalIgnoreCase)
        .ToArray();
    if (unusableSuffixes.Length > 0)
    {
        Console.Error.WriteLine(
            $"[CORS] Ignoring {unusableSuffixes.Length} unusable allowed-origin-suffix(es) " +
            $"(a suffix needs at least two labels): {string.Join(", ", unusableSuffixes)}");
    }

    options.AddPolicy(CorsPolicyName, policy =>
    {
        var builderPolicy = policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("X-Correlation-Id", "Retry-After");

        if (origins.Length == 0 && usableSuffixes.Length == 0)
        {
            return; // closed by default
        }

        // A predicate rather than WithOrigins alone, so per-commit Vercel preview hostnames match a
        // configured suffix. Exact origins are still compared literally.
        builderPolicy
            .SetIsOriginAllowed(origin =>
                origins.Contains(origin, StringComparer.OrdinalIgnoreCase)
                || corsOptions.MatchesAllowedSuffix(origin))
            .AllowCredentials();
    });
});

var app = builder.Build();

// Halt startup if the active environment is not one of Development/Staging/Production, logging an
// error identifying the unrecognized value (Req 15.6).
try
{
    OptionsRegistration.ValidateEnvironment(app.Environment);
}
catch (InvalidOperationException ex)
{
    app.Logger.LogCritical(ex, "Startup halted: {Message}", ex.Message);
    throw;
}

// Make the operator settings file discoverable in the logs: if pricing edits appear not to apply,
// the first question is always "which file is this process actually reading?".
if (operatorPropertiesPath is null)
{
    app.Logger.LogWarning(
        "No application.properties found; subscription pricing will come from appsettings/.env/" +
        "environment variables only.");
}
else
{
    app.Logger.LogInformation(
        "Operator settings loaded from {PropertiesPath} (watched for changes).",
        operatorPropertiesPath);
}

// Fixed middleware order (Req 14.1, 14.2, 14.6):
// ExceptionHandling (outermost) → CorrelationId → RequestLogging → SecurityHeaders →
// Authentication → RateLimiting → Authorization → Endpoints.
app.UseExceptionHandler();
// Resolve the real client scheme from proxy headers before any scheme-dependent middleware.
app.UseForwardedHeaders();
// HSTS + HTTPS redirection outside Development. HSTS instructs browsers to only use TLS; the
// redirect upgrades any plain-HTTP request. Skipped in Development so the local HTTP loopback and
// container health probes keep working.
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseCorrelationId();
app.UseRequestLogging();
app.UseSecurityHeaders();
app.UseCors(CorsPolicyName);
// Swagger UI + OpenAPI JSON. Placed BEFORE authentication so the docs remain reachable without a
// token (the endpoints they describe are still auth-protected at runtime). Mobile devs generate a
// typed client from /swagger/v1/swagger.json.
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "CareerPlatform API v1");
    options.RoutePrefix = "swagger";
    options.DocumentTitle = "CareerPlatform API — Swagger";
});
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

// Health endpoints are mapped anonymously so they answer under the fail-closed fallback policy
// (Req 16.5). Mapped alongside the feature endpoints.
app.MapAppHealthChecks();

// Map MVC controllers (Modular Monolith + Package-by-Feature + MVC).
app.MapControllers();

// Run the idempotent DB seeders on startup in EVERY environment so a freshly-provisioned database
// has the baseline reference rows (roles, subscription tiers, platform settings, and — only when
// an admin email is configured — the admin account). Every seeder is existence-checked by natural
// key, so repeated boots never duplicate rows. The run is fail-soft: a transient DB outage logs a
// warning instead of crashing the host.
//
// NOTE: schema management lives in `backend/schema.sql` as the single source of truth for
// tables/columns (there are intentionally no EF migration .cs files). Operators apply `schema.sql`
// to the target DB (manually or via the deploy pipeline) BEFORE the app boots; the API never runs
// `Database.MigrateAsync()`.
{
    using var scope = app.Services.CreateScope();
    try
    {
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync(CancellationToken.None);
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex,
            "Startup seed skipped: {Message}. Endpoints that require reference data will still " +
            "function, but freshly-seeded rows may be missing until the seeder runs successfully " +
            "against a schema created from schema.sql.",
            ex.Message);
    }
}

app.Run();

// Marker for WebApplicationFactory-based integration tests (populated in later tasks).
public partial class Program { }
