# CareerPlatform Backend — Target Architecture

**Pattern:** Modular Monolith · Package-by-Feature · Layered internals · MVC Controllers · ASP.NET Core 8 · PostgreSQL (via EF Core).

## 1. Guiding principles

1. **Feature is the primary unit of code organization.** The top-level tree screams what the app *does* (Courses, Mentorship, Auth) — not how it's implemented.
2. **A feature owns its stack top-to-bottom.** Controller → Service → Repository → Domain, plus feature-owned DTOs, validators, events, and DI module.
3. **Cross-feature calls go through public contracts.** A feature never reaches into another feature's internal types. Events are the loose-coupling channel; DI-registered service interfaces (in `Shared/`) are the tight-coupling channel when direct call is unavoidable.
4. **The composition root is thin.** `Program.cs` only wires the platform (auth, CORS, logging, DB, MVC) and calls `feature.Register(services, configuration)` per module.
5. **Schema is owned by `schema.sql`.** No EF migrations. Feature owners write the entity + config; the DBA-of-record updates `schema.sql`.

## 2. Top-level layout

```text
backend/
├── schema.sql                          # single source of truth for the DB
├── docs/
│   └── architecture/{structure,requirement,tasks}.md
└── src/CareerPlatform.Api/
    ├── Program.cs                      # host + pipeline + per-module Register calls
    ├── Shared/                         # cross-cutting primitives (see §4)
    │   ├── Domain/                     # Entity, AuditableEntity, DomainEvent, Result<T>, Error
    │   ├── Persistence/                # AppDbContext, interceptors, IUnitOfWork
    │   ├── Web/                        # ProblemDetails, exception handler, correlation id, security headers
    │   ├── Security/                   # ICurrentUser, JWT issuer, role policies
    │   ├── Messaging/                  # IDomainEventDispatcher, IEventHandler<T>
    │   └── Abstractions/               # ICacheClient, IStorageClient, IEmailSender, IPaymentGateway
    ├── Configuration/                  # OptionsBinding, rate limit, health checks, OpenAPI
    └── Features/
        ├── Auth/                       # one module per business capability
        │   ├── Controller/             # ONLY the HTTP surface
        │   ├── Service/                # business logic, transactional boundaries
        │   ├── Repository/             # optional; wraps DbContext for feature-owned data access
        │   ├── Domain/                 # entities, value objects, domain events
        │   ├── Dto/                    # request bodies + response DTOs (public contract)
        │   ├── Events/                 # feature-emitted events (public contract)
        │   ├── Validation/             # FluentValidation validators for request bodies
        │   └── AuthModule.cs           # DI registration for the module
        ├── Courses/                    # same layout
        ├── Mentorship/
        ├── … 22 more modules …
```

## 3. Feature module — per-file contract

Every feature contains **exactly** the folders below (empty ones may be omitted).

| Folder | What lives here | Public? |
|---|---|---|
| `Controller/` | `XxxController.cs` — MVC controller(s). Thin: `[HttpGet]` etc., binds params, calls service, returns `ActionResult<TResponse>`. **No business logic, no EF, no MediatR.** | Yes (HTTP) |
| `Service/` | `IXxxService.cs` (public contract) + `XxxService.cs` (implementation). All business logic + transactions live here. Consumes `AppDbContext` (or the feature's Repository). | Interface public; class internal |
| `Repository/` | (optional) `IXxxRepository.cs` + `XxxRepository.cs` for features whose data-access reuse is high enough to justify abstraction. Default is inline `AppDbContext` in the service. | Interface public; class internal |
| `Domain/` | EF entities, aggregate roots, value objects, and `Events/` payload records if events fire from the domain. Inherits `AuditableEntity<T>` for standard audit columns. | Entities are public; internal invariants stay in methods |
| `Dto/` | Records: `CreateXxxRequest`, `UpdateXxxRequest`, `XxxResponse`. Never expose domain entities directly. | Public |
| `Events/` | (optional) `record XxxCreated(int Id, string Slug)` — in-process events other modules can subscribe to. | Public |
| `Validation/` | FluentValidation validators for the DTOs; auto-discovered. | Internal |
| `XxxModule.cs` | Single `Register(IServiceCollection, IConfiguration)` static that wires the module's DI (services, repositories, event handlers, options). Program.cs calls it once. | Public |

### 3.1 Controller shape

```csharp
[ApiController]
[Route("api/v1/courses")]                // canonical mobile URL; legacy /api/Courses via version-rewrite middleware
public sealed class CoursesController : ControllerBase
{
    private readonly ICourseService _service;
    public CoursesController(ICourseService service) => _service = service;

    // GET /api/v1/courses
    [HttpGet]
    [AllowAnonymous]
    public Task<IReadOnlyList<CourseResponse>> List(CancellationToken ct)
        => _service.ListAsync(ct);

    // POST /api/v1/courses
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting(RateLimitPolicy.Sensitive)]
    public async Task<ActionResult<CourseResponse>> Create(
        [FromBody] CreateCourseRequest body, CancellationToken ct)
    {
        var result = await _service.CreateAsync(body, ct);
        return result.ToActionResult();          // Result<T> → 2xx / 4xx / 5xx via extension
    }
}
```

**Rules:**
- Controllers stay under ~50 lines. Anything else is service work.
- Every action method has its HTTP verb + auth + rate limit visible **on the method** — that's the entire point of the pattern.
- Return `Result<T>`-backed `ActionResult<T>` via a shared extension. Exceptions still bubble to the global exception handler.

### 3.2 Service shape

```csharp
public interface ICourseService
{
    Task<IReadOnlyList<CourseResponse>> ListAsync(CancellationToken ct);
    Task<Result<CourseResponse>> GetAsync(string slug, CancellationToken ct);
    Task<Result<CourseResponse>> CreateAsync(CreateCourseRequest req, CancellationToken ct);
    Task<Result<CourseResponse>> UpdateAsync(int id, UpdateCourseRequest req, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
}

internal sealed class CourseService : ICourseService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDomainEventDispatcher _events;
    // …one method per business use case; use FluentValidation on the request DTO before executing
}
```

- Every service method is a **use case**. Its transaction boundary is `SaveChangesAsync`.
- Cross-feature reads: query `AppDbContext` directly for read-only projections; **never** call another feature's service for reads (avoids DI cycles). If a write coordination is genuinely needed, publish a domain event.

### 3.3 Module registration

```csharp
public static class CoursesModule
{
    public static IServiceCollection RegisterCourses(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<ICourseRepository, CourseRepository>();
        // Options, event handlers, background jobs owned by this module go here.
        return services;
    }
}
```

Program.cs:
```csharp
builder.Services
    .RegisterAuth(builder.Configuration)
    .RegisterCourses(builder.Configuration)
    .RegisterMentorship(builder.Configuration)
    // …
    ;
```

## 4. Shared / platform layer

`src/CareerPlatform.Api/Shared/` holds primitives that every feature depends on but no feature owns:

| Namespace | Purpose |
|---|---|
| `Shared.Domain` | `AuditableEntity<TId>`, `Entity`, `Result` / `Result<T>`, `Error`, `IDomainEvent` |
| `Shared.Persistence` | `AppDbContext` (contains every feature's DbSet), interceptors, `IUnitOfWork` |
| `Shared.Web` | `GlobalExceptionHandler`, `ProblemDetails` conventions, `CorrelationIdMiddleware`, `SecurityHeadersMiddleware`, `ApiVersionRewriteMiddleware`, `Result<T>.ToActionResult()` extension |
| `Shared.Security` | `ICurrentUser`, JWT issuer/validator, role policy constants |
| `Shared.Messaging` | `IDomainEventDispatcher`, `IEventHandler<T>`, in-process pub-sub |
| `Shared.Abstractions` | `ICacheClient`, `IStorageClient`, `IEmailSender`, `IPaymentGateway` — one concrete adapter per interface |

**Rule:** `Features/*` may depend on `Shared/*`. `Shared/*` never depends on `Features/*`. Enforced by an updated architecture test.

## 5. Routing / URL policy (unchanged from the current API-versioning work)

- **Canonical:** `/api/v1/{feature}` — lowercase kebab-case plural nouns. This is the URL mobile clients call.
- **Legacy:** `/api/{PascalCase}` — supported via `ApiVersionRewriteMiddleware` for the existing frontend during rollout.
- **HTTP methods:** `GET` read · `POST` create/RPC · `PUT` full update · `DELETE` remove. No `PATCH`.
- **Auth:** Bearer JWT (Supabase). Global fallback = require-authenticated. Feature actions declare `[AllowAnonymous]` / `[Authorize(Roles = "…")]` explicitly.

## 6. Tests layout

```text
tests/
├── CareerPlatform.ArchitectureTests/       # per §7 conventions
├── CareerPlatform.UnitTests/{Feature}/     # per-feature unit tests mirror the feature folder
└── CareerPlatform.IntegrationTests/{Feature}/  # per-controller end-to-end via WebApplicationFactory
```

## 7. Architecture invariants (replace the current arch tests)

| Rule | Test |
|---|---|
| Every controller inherits `ControllerBase` and lives under `Features/*/Controller` (namespace ends `.Features.<Feature>.Controller`) | `ControllerLocationTests` |
| Every feature under `Features/` has only layered folders (`Controller/Service/Dto/Validation/Domain/Repository/Events/Lifecycle`); any feature with an HTTP surface has a `{Feature}Module.cs` | `SliceStructureTests` (rewritten) |
| Every mapped endpoint declares `[Authorize]` or `[AllowAnonymous]` explicitly | `ExplicitAuthorizationTests` |
| Feature services depend only on abstractions or `AppDbContext` (no concrete infrastructure adapter) | `AbstractionDependencyTests` (retargeted to `ServiceTypes`) |
| Entities inherit `Entity<TId>` | `DomainBaseTypeTests` |
| Repository layout — `src/CareerPlatform.Api/Features` etc., test projects, docs subdirectories, root config files | `LayoutCompletenessTests` |

Tests deleted at the end of Phase 3 (MediatR-specific, no equivalent needed once handlers are gone): `HandlerConventionTests`, `ResultReturnTypeTests`, `ValidatorConventionTests`. The file `NoMvcControllerTests.cs` was renamed to `ControllerLocationTests.cs` (its class was already renamed).

Validators are still discovered by `AddValidatorsFromAssembly` and invoked automatically by the global `FluentValidationActionFilter` (registered in `AddControllers`). No MediatR pipeline behavior is needed anymore.

## 8. What stays the same

- Postgres schema (`schema.sql`) — untouched.
- HTTP URL surface — every existing route keeps working; new canonical form is `/api/v1/*`.
- Auth model (Supabase JWT, roles).
- Rate limiting, correlation id, structured logging, health checks, Swagger.
- Frontend `endpoints.ts` — no changes needed.
