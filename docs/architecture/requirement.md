# Backend Revamp — Requirements & Acceptance Criteria

## 1. Objective

Refactor the CareerPlatform backend from **Vertical Slice + Minimal APIs + MediatR** to **Modular Monolith · Package-by-Feature · Layered internals · MVC Controllers**, matching the reference architecture in `structure.md`.

## 2. Scope

**In scope**
- All 141 endpoints across 26 feature modules (`Features/*`).
- `Program.cs` composition root.
- Architecture tests.
- Unit & integration tests (adjusted to call controllers instead of MediatR handlers).

**Out of scope**
- Database schema (`schema.sql` unchanged).
- Business logic (behavior preserved verbatim).
- Frontend / mobile / third-party clients (URL contract preserved).
- Authentication scheme (Supabase JWT unchanged).
- Cross-cutting infrastructure (rate limiting, health checks, observability, Swagger, versioning middleware, seed data).

## 3. Functional requirements

- **F1.** Every current URL continues to resolve, with the same HTTP method, same auth, same request body, and byte-identical response shape.
- **F2.** New canonical URL is `/api/v1/{kebab-case-resource}`. Legacy `/api/{PascalCase}` continues to work through `ApiVersionRewriteMiddleware`.
- **F3.** Every controller action carries **explicit** `[Authorize(...)]` or `[AllowAnonymous]`. The application-wide fallback policy still enforces authentication as the default.
- **F4.** Rate-limited endpoints (currently `.RequireRateLimiting("Sensitive")`) carry `[EnableRateLimiting(RateLimitPolicy.Sensitive)]`.
- **F5.** Validation still runs before service logic. FluentValidation validators live under `Features/{X}/Validation/` and are auto-discovered.
- **F6.** Domain events emitted by services (e.g. `CourseCreated`) reach subscribers registered in other modules (in-process, best-effort).

## 4. Non-functional requirements

- **N1. Build cleanliness.** After every phase, `dotnet build CareerPlatform.sln` returns 0 errors and 0 warnings.
- **N2. Test coverage.** After every phase, every existing passing test still passes; every new controller has at least one unit test on the service and one integration test on the controller.
- **N3. Backwards compatibility during rollout.** Old vertical slices and new modules **may coexist** feature-by-feature while migration is in flight. No global cut-over.
- **N4. No mass reformatting.** Only files touched by the migration change; noise diffs are forbidden.
- **N5. No data-plane risk.** No EF migrations. Schema changes (if any prove necessary) are appended to `schema.sql` as idempotent DDL and reviewed separately.

## 5. Migration strategy

1. **Phase 0 — Foundations.** Wire MVC (`AddControllers` / `MapControllers`), introduce `Shared/` folder + `Result<T>.ToActionResult()` extension, relax the two blocker arch tests (`NoMvcControllerTests`, `SliceStructureTests`) so the two styles may coexist during rollout.
2. **Phase 1 — Pilot.** Convert one representative feature end-to-end (`Courses`, 5 endpoints) as reference implementation. Delete its 5 slice folders only when the new controller passes.
3. **Phase 2 — Rollout.** Convert the remaining 25 feature modules in the sequence defined in `tasks.md`. Simple read-only features first, feature families with cross-links last.
4. **Phase 3 — Cleanup.** Once every feature is migrated: remove MediatR, `IEndpoint`, `IEndpoint`-registration reflection, legacy arch tests. Re-add the "no legacy slices" arch test.
5. **Phase 4 — Verification.** Full solution build; a curl-based smoke script hits every route; frontend smoke; mobile team green-lit.

## 6. Acceptance criteria (per feature)

A feature is "done" when **all** of the following hold:

- [ ] `Features/{Feature}/Controller/{Feature}Controller.cs` exists and covers every legacy route with the same auth + rate limits.
- [ ] `Features/{Feature}/Service/I{Feature}Service.cs` + `{Feature}Service.cs` own the business logic.
- [ ] DTOs in `Features/{Feature}/Dto/`. Request DTOs have FluentValidation validators.
- [ ] `{Feature}Module.cs` calls `AddScoped<...>` for each abstraction and is invoked in `Program.cs`.
- [ ] All 5 legacy `Features/{Feature}/{Op}/*.cs` slice folders are deleted.
- [ ] `dotnet build` = 0 errors / 0 warnings.
- [ ] All existing tests referencing this feature pass; new unit test on the service; new integration test on the controller.
- [ ] `curl -i https://.../api/v1/{feature}` and `.../api/{Legacy}` both return the same status + body shape.

## 7. Overall acceptance (project-wide)

- [ ] Zero types remaining in the API assembly implement `IEndpoint` or `IRequestHandler<...>`.
- [ ] MediatR + `IEndpoint` infrastructure removed from `CareerPlatform.Api.csproj`.
- [ ] `NoLegacySlicesTests` (new arch test) is green.
- [ ] `structure.md` matches the built code.
- [ ] Frontend regression suite passes without changes.
- [ ] Mobile team can generate a typed client from `/swagger/v1/swagger.json` and every action succeeds against a real backend.

## 8. Risks & mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Regression in preserved behavior | High | Integration test per controller before deleting slice; keep legacy routes live during migration. |
| MVC + Minimal API pipeline conflict on the same route | High | Migrate feature-by-feature; delete the legacy `Endpoint.cs` only when the new controller ships and its tests are green. |
| Domain event coupling between features not previously wired | Medium | Introduce `IDomainEventDispatcher` in Phase 0; features emit events but no subscribers until the consuming feature is migrated. |
| Arch tests block progress | Medium | Phase 0 relaxes the blockers; Phase 3 re-tightens with new rules. |
| Session context budget | Medium | tasks.md is written so each feature is a self-contained session's worth of work. |

## 9. What "done" looks like — one-line summary

> Every action any client can call is defined by an `[HttpVerb("route")]`-decorated method on a controller under `Features/{Feature}/Controller/`, calls a single method on `I{Feature}Service`, and returns an `ActionResult<T>` — with all 141 URLs preserved and every arch test green.
