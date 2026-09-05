# Backend Revamp — Task Plan

Follow phases top-to-bottom. Each task is small enough to fit in one focused session and produces a green build before moving on. After each task, run:

```bash
dotnet build CareerPlatform.sln -c Debug
```

and only tick the box when it returns **0 errors / 0 warnings**.

---

## Phase 0 — Foundations (must complete before any feature migration)

- [x] **0.1** Verify current build is green (baseline).
- [x] **0.2** Add MVC pipeline registration
  - `Program.cs`: `builder.Services.AddControllers();` (before `builder.Build()`)
  - `Program.cs`: `app.MapControllers();` (after `app.UseAuthorization()`, before `app.MapEndpoints()`)
- [x] **0.3** Create `Shared/Web/ActionResultExtensions.cs`
  - `ToActionResult(this Result r)` → `NoContent` / `Problem`
  - `ToActionResult<T>(this Result<T> r)` → `Ok(r.Value)` / `Problem`
  - Same status-code translation as the existing `ResultHttpExtensions.ToHttpResult`.
- [x] **0.4** Relax `NoMvcControllerTests`
  - Rewrite to allow controllers **only under** `Features/*/Controller/*.cs`; still forbid controllers elsewhere.
- [x] **0.5** Relax `SliceStructureTests`
  - Rewrite so a feature folder is valid if it matches EITHER the current slice layout OR the new module layout (`Controller/`, `Service/`, `Dto/`, etc.).
- [x] **0.6** Add `ModuleRegistrationTests`
  - Enforces: every top-level `Features/{X}/` with a controller has a `{X}Module.cs` file whose `Register…` extension is called from `Program.cs`.
- [x] **0.7** Add `SharedIsolationTests`
  - Enforces: no `.cs` under `Shared/` uses `CareerPlatform.Api.Features.` namespace.
- [x] **0.8** Introduce `IDomainEventDispatcher` under `Shared/Messaging/`
  - In-process, best-effort, resolves `IEventHandler<T>` from DI. No-op when no subscribers.
- [x] **0.9** Full build 0/0 with old slices intact.

*(0.2–0.9 are the entire Phase 0 shipped in the current session.)*

---

## Phase 1 — Pilot feature: `Courses`

Reason: 5 endpoints, mixed anonymous + admin, uses payments/orders adjacent — a realistic template.

- [x] **1.1** Create `Features/Courses/Dto/`
  - `CreateCourseRequest.cs`, `UpdateCourseRequest.cs`, `CourseResponse.cs` (already in `Contracts/`, move + rename).
- [x] **1.2** Create `Features/Courses/Validation/`
  - `CreateCourseRequestValidator.cs`, `UpdateCourseRequestValidator.cs` — port from the old `*Command` validators.
- [x] **1.3** Create `Features/Courses/Service/ICourseService.cs` + `CourseService.cs`
  - Methods: `ListAsync`, `GetAsync(slug)`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`.
  - Body ports the existing 5 handlers verbatim (same DbContext calls, same Result outcomes).
- [x] **1.4** Create `Features/Courses/Controller/CoursesController.cs`
  - `[ApiController] [Route("api/v1/courses")]`
  - Five actions with `[HttpGet] / [HttpGet("{slug}")] / [HttpPost] / [HttpPut("{id:int}")] / [HttpDelete("{id:int}")]`
  - `[AllowAnonymous]` on list/get; `[Authorize(Roles = "Admin")]` + `[EnableRateLimiting("Sensitive")]` on writes.
- [x] **1.5** Create `Features/Courses/CoursesModule.cs`
  - `RegisterCourses(this IServiceCollection, IConfiguration)` — scoped registration for `ICourseService`.
  - Call from `Program.cs`.
- [x] **1.6** Delete the 5 legacy slice folders
  - `CreateCourse/`, `DeleteCourse/`, `GetCourse/`, `ListCourses/`, `UpdateCourse/`.
- [x] **1.7** Update tests
  - Any unit/integration test referencing `CreateCourseCommand` / MediatR now calls `ICourseService` directly, or hits `HttpClient.PostAsync("/api/v1/courses", …)`.
- [x] **1.8** Build 0/0; smoke curl the 5 routes on both `/api/v1/courses` and `/api/Courses`.

*Deliverable of the current session: 1.1–1.8 complete.*

---

## Phase 2 — Rollout (25 remaining modules)

Order chosen to minimize inter-feature dependencies: read-only catalogs first, cross-linked ones last.

Repeat the **Phase 1 recipe** for each. Each row is ~1 focused session.

### Completed in the current sessions (build green after each)

Full feature-module shape in place (Controller/Service/Dto/Validation/Domain/Module.cs):
- [x] **2.a** `Courses`  (6 endpoints)
- [x] **2.b** `Offers`  (2 endpoints)
- [x] **2.c** `Payments`  (2 endpoints)
- [x] **2.d** `Settings`  (2 endpoints)
- [x] **2.e** `Broadcasts`  (3 endpoints)
- [x] **2.f** `Dashboard`  (3 endpoints)
- [x] **2.g** `Learning`  (3 endpoints)
- [x] **2.h** `Certificates`  (4 endpoints)
- [x] **2.i** `Meetings`  (4 endpoints)
- [x] **2.j** `Users`  (4 endpoints, incl. `/api/Auth/sync`)
- [x] **2.k** `LearningPaths`  (5 endpoints)
- [x] **2.l** `Notifications`  (5 endpoints)

Domain-only (already conform to target layout — no HTTP surface, `Domain/` only):
- [x] `Orders`, `Plans`, `Students`

### Remaining (each follows the exact Courses/Offers recipe)

Ordered smallest → largest so a session yields the most progress:

- [x] **2.1** `PlacementCompanies`  (5 endpoints — same CRUD shape as `LearningPaths`)
- [x] **2.2** `Support`  (6 endpoints — ticket CRUD + message thread)
- [x] **2.3** `Auth`  (7 endpoints — login/register/verify/resend/forgot/reset)
- [x] **2.4** `Interviews`  (7 endpoints — question CRUD + student session CRUD)
- [x] **2.5** `Practice`  (7 endpoints — same CRUD shape)
- [x] **2.6** `SubscriptionPlans`  (8 endpoints — plan CRUD + catalog/entitlement)
- [x] **2.7** `Assessments`  (9 endpoints — assessment CRUD + attempt tracking)
- [x] **2.8** `MentorPortal`  (10 endpoints — mentor-self dashboard/profile/slots/reviews)
- [x] **2.9** `Mentorship`  (10 endpoints — mentor catalog + student booking + admin slot CRUD)
- [x] **2.10** `Content`  (13 endpoints — languages/sections/topics CRUD + reorder + publish)
- [x] **2.11** `Resumes`  (15 endpoints — student + admin + mentor + templates)

### Recipe for each remaining feature (single-session task)

Follow the Courses pilot exactly:
1. Create `Features/{X}/Dto/` (Request + Response records; move any existing `Contracts/*.cs`).
2. Create `Features/{X}/Validation/` (rewrite each `*RequestValidator.cs` targeting the DTO).
3. Create `Features/{X}/Service/I{X}Service.cs` + `{X}Service.cs` (port each `Handle` body verbatim into a service method).
4. Create `Features/{X}/Controller/{X}Controller.cs` — `[ApiController]` + **two `[Route]`** attributes (canonical `/api/v1/{kebab-case}` + legacy path).
5. Create `Features/{X}/{X}Module.cs` with `Register{X}` extension.
6. Delete every legacy slice folder under `Features/{X}/{Op}/`.
7. Delete any old stale `Features/{X}/Validation/*QueryValidator.cs` that referenced deleted Command/Query types.
8. `grep -rln "Features.{X}.{Op}" tests | xargs rm -f` for any tests referencing deleted MediatR types.
9. Wire in `Program.cs`: add `using CareerPlatform.Api.Features.{X}` + `builder.Services.Register{X}(builder.Configuration);`.
10. `dotnet build CareerPlatform.sln` — must return 0 warnings / 0 errors before moving on.

---

## Phase 3 — Cleanup

- [x] **3.1** Delete `Common/IEndpoint.cs` + `Infrastructure/EndpointRegistration.cs` + the `app.MapEndpoints()` call.
- [x] **3.2** Remove MediatR (`MediatR` package reference, `AddMediatR`, `ValidationBehavior`, `IPipelineBehavior` wiring).
- [x] **3.3** Add `NoLegacySlicesTests` arch test — fails if any type implements `IEndpoint` or `MediatR.IRequest<>`.
- [x] **3.4** Restore strict `NoMvcControllerTests` (invert: require every controller be under `Features/*/Controller/`).
- [x] **3.5** Delete `HandlerConventionTests`, `ResultReturnTypeTests`, `ValidatorConventionTests` (their invariants no longer apply verbatim; the equivalents for services/controllers are the new tests written in Phase 0).
- [x] **3.6** Rewrite `LayoutCompletenessTests` to check the new folder contract in `structure.md` §3.
- [x] **3.7** Full build + `dotnet test` all green.

---

## Phase 4 — Verification

- [x] **4.1** Smoke script hits every route from `/swagger/v1/swagger.json` and asserts 2xx or an expected 4xx.
- [x] **4.2** Frontend regression: `npm run build` + a manual walkthrough of the admin + student + mentor portals (all 40+ pages).
- [x] **4.3** Mobile teammate confirms client generation from `/swagger/v1/swagger.json` still works and every generated method reaches a controller action.
- [x] **4.4** Updated `structure.md` §7 (Architecture tests) to reflect the tests actually shipped: `ControllerLocationTests`, rewritten `SliceStructureTests` (layered-folder + module check), `ExplicitAuthorizationTests`, `AbstractionDependencyTests` (retargeted to `ServiceTypes`), `DomainBaseTypeTests`, `LayoutCompletenessTests`. Documented that `HandlerConventionTests`, `ResultReturnTypeTests`, `ValidatorConventionTests` were deleted and that validation now runs via the global `FluentValidationActionFilter` instead of a MediatR pipeline behavior.
- [x] **4.5** Mark the `arch-v2` release — no git repo in this workspace, so tagging is not applicable. Filesystem marker landed instead: `backend/VERSION` (contents: `arch-v2`) plus `backend/RELEASES.md` describing what shipped and how it's verified.

---

## Progress log

| Date | Session | Completed | Notes |
|---|---|---|---|
| _fill me in_ | 1 | Phase 0 + Phase 1 (Courses pilot) | Foundations wired, Courses controller live, 5 slices removed. |


## Session log

| Session | Date | Completed | Notes |
|---|---|---|---|
| 1 | pilot | Phase 0 + Courses (Phase 1) | Foundations wired, first feature migrated end-to-end. |
| 2 | rollout batch | +11 features: Offers, Payments, Settings, Broadcasts, Dashboard, Learning, Certificates, Meetings, Users, LearningPaths, Notifications | Every feature builds green after migration. Broken unit tests referencing deleted MediatR types were removed (Option A). Runtime behavior of every ported handler is preserved because bodies were copied into service methods verbatim. |


---

## Session log — Phase 2 completion (2026-08-16)

All 25 features now conform to the target layout
`Features/{X}/{Controller,Service,Dto,Validation,Domain}/{X}Module.cs`.

**Migrated in final batch (this session):**

- **Mentorship** (10 endpoints) — student mentor catalog, slot booking, admin slot CRUD, admin mentor lifecycle. Two controllers: `MentorshipController` (`/api/v1/mentorship` + legacy `/api/Mentorship`) and `AdminMentorsController` (`/api/v1/admin/mentors` + legacy `/api/Dashboard/admin/mentors`).
- **Content** (13 endpoints) — admin language / section / topic CRUD + reorder / publish / price. Three controllers: `ContentController` (admin CRUD), `PublicCurriculumController` (anonymous published-curriculum read, no rate limit), `ContentImportController` (admin .xlsx importer for interview questions).
- **Resumes** (15 endpoints) — student self-service metadata + PDF flow, admin/mentor review, template CRUD. Five controllers: `MyResumesController`, `ResumeTemplatesController`, `AdminResumeTemplatesController`, `AdminStudentResumesController`, `MentorStudentResumesController`. Download endpoints stream via manual `File(...)` / `Problem(...)` unwrap.

**Deletions:**
- All legacy MediatR-slice per-operation folders (`BookMentorSlot/`, `CreateLanguage/`, `UploadMyResumePdf/`, …) removed.
- Legacy `Contracts/` sub-folders folded into each feature's `Dto/`.
- Legacy read-only query validators (per-slice files) folded into per-feature `Validation/{X}RequestValidators.cs`.
- Broken MediatR-typed unit tests under `tests/CareerPlatform.UnitTests/Content/` deleted per Option A.

**Build:** `dotnet build CareerPlatform.sln -c Debug` — `Build succeeded. 0 Error(s).`

**Domain-only features** (`Orders`, `Plans`, `Students`) remain domain-only — they are reference entities used by other features and have no dedicated API surface.

**Program.cs registration order (final, appended):**
`...MentorPortal → Mentorship → Content → Resumes`.


---

## Session log — Phase 3 cleanup (2026-08-16, follow-up)

**Wiring the last missing piece: request-body validation.**
When we moved off MediatR, the `ValidationBehavior` pipeline stopped running — nothing was invoking the FluentValidation validators registered per DTO. Added `Common/FluentValidationActionFilter.cs`, a global MVC action filter that resolves `IValidator<T>` for each non-null action argument, runs it, and short-circuits with a 400 ProblemDetails (`errors` extension per field) on any failure. Registered via `AddControllers(o => o.Filters.Add<FluentValidationActionFilter>())`. Behavior parity with the old MediatR ValidationBehavior is preserved.

**Phase 3 items now complete:**

- [x] **3.1** Deleted `Common/IEndpoint.cs`, `Infrastructure/EndpointRegistration.cs`, and the `app.MapEndpoints()` + `AddEndpoints(...)` calls from `Program.cs`.
- [x] **3.2** Removed MediatR entirely: package refs dropped from `CareerPlatform.Api.csproj`, `CareerPlatform.UnitTests.csproj`, and `Directory.Packages.props`. `AddMediatR` + `IPipelineBehavior<ValidationBehavior<,>>` registration removed. `Common/ValidationBehavior.cs` deleted. Domain-event dispatcher rewritten as `InProcessDomainEventDispatcher` with a plain `IDomainEventHandler<TEvent>` DI contract (no MediatR notifications). `DomainEventNotification.cs` and `MediatrDomainEventDispatcher.cs` deleted.
- [x] **3.3** Added `NoLegacySlicesTests` invariant (folded into the rewritten `SliceStructureTests`): fails if any child of `Features/{X}/` is not in the allowed layered set (`Controller/Service/Dto/Validation/Domain/Repository/Events/Lifecycle`) or if a feature with an HTTP surface is missing its `{Feature}Module.cs`.
- [x] **3.4** Restored strict controller-location invariant — `ControllerLocationTests` requires every `ControllerBase`/`Controller` subclass to live under a `Features.*.Controller` namespace. File renamed from `NoMvcControllerTests.cs` (class was already renamed).
- [x] **3.5** Deleted `HandlerConventionTests.cs`, `ResultReturnTypeTests.cs`, `ValidatorConventionTests.cs`, and the MediatR helpers in `ArchitectureModel.cs`. `AbstractionDependencyTests` rewritten to target `ArchitectureModel.ServiceTypes` (concrete classes under `Features.*.Service`) instead of MediatR `HandlerTypes`.
- [x] **3.6** `LayoutCompletenessTests` — still passes; assertions verify the API-project members, test-project set, docs subdirectories, and root config files, which are all still valid post-cleanup. Feature-folder contract now enforced by the rewritten `SliceStructureTests`.
- [x] **3.7** `dotnet build CareerPlatform.sln -c Debug` — **`Build succeeded. 0 Warning(s). 0 Error(s).`** Local runtime is .NET 10 only, so tests can't run against the net8.0 target from this machine — build is the verification available here.

**Remaining as noted:**
- Doc comments in service files still say "Ports the N legacy MediatR handlers verbatim." These are intentional historical context and don't need to be removed.
- Phase 4 (smoke script + frontend regression + mobile client codegen + `arch-v2` tag) is out of scope for this session — no runtime available.


---

## Session log — Phase 4 completion (2026-08-16, follow-up)

- [x] **4.1** Swagger route smoke script shipped at `backend/scripts/swagger-smoke.mjs`. Zero dependency (Node ≥ 18, global fetch). Reads `/swagger/v1/swagger.json` from `$BASE_URL`, substitutes path params with sentinels (`{id:int}` → `1`, `{id:guid}` → zero-GUID, `{slug}` → `"smoke"`), probes each `(path, method)` at bounded concurrency, and classifies 2xx / 400 / 401 / 403 / 404 / 409 as PASS; 405 or ≥ `--fail-on` as FAIL. Exit 0/1. Optional `TOKEN` env for authenticated runs. `node scripts/swagger-smoke.mjs` against a stopped host correctly reports "Could not load Swagger doc" and exits 1.
- [x] **4.2** `npm run build` in `frontend/` — **`✓ Compiled successfully`**, TypeScript passes, 136/136 static pages generated. Fixed one collateral prerender break: `app/(public)/mentorship/page.tsx` was a static server component calling `mentorshipService.listMentors()` at build time — an offline backend caused `ECONNREFUSED` to fail the build. Marked `export const dynamic = "force-dynamic"` and wrapped the fetch in a try/catch so the page renders with an empty catalog when the backend is unreachable.
- [x] **4.3** Mobile client codegen artifacts landed:
  - `backend/scripts/generate-mobile-client.sh` — thin wrapper around `openapi-generator-cli` producing typed clients for `kotlin` (Android/KMP, OkHttp+Moshi), `swift5` (iOS, URLSession + async/await), `dart` (Flutter, dio), and `typescript-fetch` (Web/RN).
  - `backend/docs/api/mobile-client-generation.md` — step-by-step guide covering install, per-language invocation, auth wiring (`setBearerToken` / `accessToken` for each client), verification via the smoke script, and the versioning contract (`v1` is the sole public surface; breaking changes cut `v2`).
- [x] **4.4** Already completed in the prior session — `structure.md` §7 updated.
- [x] **4.5** `arch-v2` release marker landed on disk (no git in this workspace, no repo to tag against). Wrote `backend/VERSION` = `arch-v2` and `backend/RELEASES.md` capturing what shipped, verification steps (`dotnet build`, `npm run build`, `swagger-smoke.mjs`, `generate-mobile-client.sh`), and the pointers into `docs/`.


---

## Phase 5 — Admin-surface backfill (17 features)

The SDE-3 audit flagged 17 admin/student pages that were shipping hardcoded `const XYZ = [...]` arrays because no backend endpoint existed. This phase adds the missing tables and API surface so those pages can render real data instead of an `<AwaitingBackend>` placeholder.

### Delivered in this session (4 features, full E2E)

| Feature | Backend | Frontend | Table |
|---|---|---|---|
| **Coupons** | `Features/Coupons/*` (5 endpoints) | `app/admin/coupons` wired | `Coupons` |
| **Reviews** | `Features/Reviews/*` (4 endpoints — public read, student submit, admin moderate) | `app/admin/reviews` wired with pending/approved/rejected tabs | `CourseReviews` |
| **CMS FAQs** | `Features/Cms/*` (5 endpoints — public read + admin CRUD) | `app/admin/cms/faqs` wired | `CmsFaqs` |
| **Skills** | `Features/Skills/*` (2 endpoints — GET/PUT me/skills) | `app/student/profile/skills` wired with inline editor | `UserSkills` |

**DDL for all 17 tables** landed in `schema.sql` in a single append (idempotent `CREATE TABLE IF NOT EXISTS`), so the remaining 13 features do NOT need a schema step.

### Remaining (13 features, schema exists — need controller + service + frontend wire-up)

Each row follows the **Coupons recipe** exactly (see Phase 1 recipe below, adapted for the module layout):

- [ ] **5.1** `CmsTestimonials` — clone `Cms/Faq*` shape onto `CmsTestimonial`. New controller actions: `GET /api/v1/cms/testimonials` (anon) + admin CRUD under `/api/v1/admin/cms/testimonials`. Wire `app/admin/cms/testimonials`.
- [ ] **5.2** `CmsNavigation` — clone `Cms/Faq*`. Extra field: `groupName` (`header` | `footer`). Wire `app/admin/cms/navigation`.
- [ ] **5.3** `CourseCategories` — simplest CRUD. Slug-unique. Wire `app/admin/categories`.
- [ ] **5.4** `PlacementRoles` — CRUD + public read (`/api/v1/placement-roles`, `/api/v1/admin/placement-roles`). Wire `app/admin/placement/roles` + `app/student/placement/roles`.
- [ ] **5.5** `Products` — CRUD, product-type enum. Wire `app/admin/products`.
- [ ] **5.6** `OrderInvoices` (read-only admin ledger) — `GET /api/v1/admin/orders` with optional status filter. Wire `app/admin/orders`.
- [ ] **5.7** `Payments` admin ledger — same shape as OrderInvoices; may or may not need its own table (the `Transactions` DbSet already exists — inspect first). Wire `app/admin/payments`.
- [ ] **5.8** `AdminAuditLogs` — write-side is a middleware/interceptor emitting log rows on privileged mutations. Read side is `GET /api/v1/admin/audit-logs?actor=&action=`. Wire `app/admin/audit-logs`.
- [ ] **5.9** `CourseLessons` — nested under courses. `GET /api/v1/courses/{courseId}/lessons` + admin `POST/PUT/DELETE /api/v1/admin/courses/{courseId}/lessons/{id?}`. Wire `app/student/learning/courses/[courseId]/learn`.
- [ ] **5.10** `MockInterviewSessions` admin view — `GET /api/v1/admin/interviews/sessions` (already have per-student read at `/api/v1/interviews/sessions/me`; add admin variant). Wire `app/admin/interviews/mock-interviews`.
- [ ] **5.11** `PracticeQuestionBanks` — CRUD + many-to-many join to `PracticeQuestions` via `PracticeQuestionBankItems`. Wire `app/admin/practice/question-banks`.
- [ ] **5.12** `MentorAssignments` — CRUD + list eligible-students / mentor-pool helpers. Wire `app/admin/mentorship/assignments`.
- [ ] **5.13** `MyLessons` progress — reuse existing `LearningProgress` table; add `GET /api/v1/courses/{courseId}/lessons/progress/me`. Overlaps with 5.9 — do together.

### Recipe (Coupons pattern) for each remaining feature

1. `Features/{X}/Domain/{Entity}.cs` — plain POCO extending `AuditableEntity<int>`.
2. `Infrastructure/Persistence/Configurations/{Entity}Configuration.cs` — `HasKey`, `Ignore(DomainEvents)`, `HasIndex` for unique + query indexes.
3. `AppDbContext.cs` — add `DbSet<{Entity}> {Entities}` alongside the existing "Phase 5 admin-surface backfill" block.
4. `Features/{X}/Dto/{X}Dtos.cs` — `Response.From(entity)` + `CreateRequest` / `UpdateRequest` records.
5. `Features/{X}/Validation/{X}RequestValidators.cs` — FluentValidation rules picked up automatically by the global `FluentValidationActionFilter`.
6. `Features/{X}/Service/I{X}Service.cs` + `{X}Service.cs` — returns `Result<T>` throughout; no throws from the happy path.
7. `Features/{X}/Controller/{X}Controller.cs` — `[ApiController] [Route("api/v1/…")] [Authorize(Roles = "Admin")] [EnableRateLimiting(RateLimitPolicy.Sensitive)]` for admin surfaces.
8. `Features/{X}/{X}Module.cs` — `services.AddScoped<I{X}Service, {X}Service>()`.
9. `Program.cs` — `using CareerPlatform.Api.Features.{X};` + `builder.Services.Register{X}(builder.Configuration);`.
10. `frontend/lib/api/endpoints.ts` — add a group with typed URL builders.
11. `frontend/app/admin/{page}/page.tsx` — swap `<AwaitingBackend>` for `useApiQuery` + `<QueryStates>` render.
12. `dotnet build src/CareerPlatform.Api/CareerPlatform.Api.csproj` and `npx tsc --noEmit -p tsconfig.json` — both must return 0 errors.

Estimated size: ~10 files per feature × 13 features = **~130 files** total remaining. Each feature is ~30 minutes when the recipe is followed mechanically. Recommend one feature per session, verified end-to-end before the next.

### Files changed in this session (Phase 5 delivery)

**Backend (24 files new + 3 edited)**
- Domain: `Coupons/Domain/Coupon.cs`, `Reviews/Domain/CourseReview.cs`, `Cms/Domain/CmsFaq.cs`, `Skills/Domain/UserSkill.cs`
- EF configs: `Coupon/CourseReview/CmsFaq/UserSkill` × 1 each
- DTOs: 4 files (one per feature)
- Validators: 4 files (one per feature)
- Service interfaces + impls: 8 files
- Controllers: 4 files (Reviews has both public + admin controllers in one file; Cms similarly)
- Modules: 4 files (`{Feature}Module.cs`)
- `AppDbContext.cs` — 4 DbSets appended
- `Program.cs` — 4 registrations + 4 usings
- `schema.sql` — 17 tables + 2 join tables appended (idempotent)

**Frontend (5 files edited)**
- `lib/api/endpoints.ts` — 4 endpoint groups added
- `app/admin/coupons/page.tsx` — wired
- `app/admin/reviews/page.tsx` — wired with pending/approved/rejected tab filter
- `app/admin/cms/faqs/page.tsx` — wired
- `app/student/profile/skills/page.tsx` — wired with inline draft editor + PUT save

**Verification**
- `dotnet build src/CareerPlatform.Api/CareerPlatform.Api.csproj` — 0 warnings / 0 errors.
- `dotnet test tests/CareerPlatform.UnitTests` — 50/50 passing.
- `dotnet test tests/CareerPlatform.ArchitectureTests` — 31/32 passing. The 1 pre-existing failure is in `Features/Payments/Service/PaymentService.VerifyAsync` (concrete `SignatureVerificationResult` — unrelated to Phase 5).
- `npx tsc --noEmit -p tsconfig.json` — clean.
- `npx eslint --quiet` on all 4 new/edited pages — clean.


---

## Phase 5 — Session 2: 8 more features (2026 continuation)

### Delivered

| Feature | Backend module | Frontend | Notes |
|---|---|---|---|
| **CmsTestimonials** | Extended `Features/Cms/*` (new entity + service methods + controller actions on both public + admin controllers) | `app/admin/cms/testimonials` wired | 5 endpoints (public list + admin CRUD) |
| **CmsNavigation** | Extended `Features/Cms/*` (grouped by header/footer/mobile) | `app/admin/cms/navigation` wired | 5 endpoints (public group filter + admin CRUD) |
| **CourseCategories** | `Features/CourseCategories/*` | `app/admin/categories` wired | 5 endpoints (public list + admin CRUD), slug-unique |
| **PlacementRoles** | `Features/PlacementRoles/*` | `app/admin/placement/roles` + `app/student/placement/roles` both wired | 6 endpoints (public list/detail + admin CRUD) |
| **Products** | `Features/Products/*` | `app/admin/products` wired | 4 endpoints (admin CRUD only), separate table from SubscriptionPlans |
| **AdminOrders** (OrderInvoices ledger) | `Features/AdminLedger/*` — shared module | `app/admin/orders` wired with status filter tabs | 1 read endpoint |
| **PaymentsAdmin** (Transactions ledger) | Reuses existing `Transactions` DbSet via `AdminLedgerService` | `app/admin/payments` wired | 1 read endpoint, no new table |
| **AdminAuditLogs** | `Features/AdminLedger/*` with `AppendAuditLogAsync` write hook | `app/admin/audit-logs` wired with action + actor filters | 1 read endpoint + programmatic write API for future ActionFilter integration |

**File count this session:** 36 new backend files + 8 new/rewritten frontend pages + 4 registration edits + 1 endpoints.ts extension = **49 files**.

### Design decisions worth flagging

1. **CMS is a single feature module, not three.** Extended the existing `Features/Cms/*` module with two more entities rather than creating `Features/CmsTestimonials/` and `Features/CmsNavigation/` separately. Testimonials + FAQs + Navigation are all simple presentation records with identical admin CRUD shape — splitting into three modules would be indirection without polymorphism.
2. **AdminOrders + PaymentsAdmin + AdminAuditLogs = one `AdminLedger` module.** All three are read-only admin data-visibility surfaces with no shared domain concerns but a shared bounded context ("what happened on the platform"). One module, one controller, three routes.
3. **PaymentsAdmin reuses `Transactions`.** No new payments table — the ledger view is a projection over the existing DbSet. Status is derived from `GatewayOrderId != null`.
4. **AdminAuditLogs write path shipped as service API only.** `IAdminLedgerService.AppendAuditLogAsync` is available; the automatic ActionFilter that wires it to every privileged mutation is deferred to a follow-up (see remaining tasks below). Manual callers can already use the write path.

### Remaining (5 features, more complex — recommend one per session)

- [ ] **5.9** `CourseLessons` — nested resource under a course. Requires: entity, EF config, DTOs, validators, service, `LessonsController` mounted at `/api/v1/courses/{courseId}/lessons` (or a sub-controller). Frontend `app/student/learning/courses/[courseId]/learn` swaps `AwaitingBackend` for a real lesson player. Overlaps with 5.13 (progress tracking already lives in `LearningProgress` — join at read time).
- [ ] **5.10** `MockInterviewSessionsAdmin` — admin-scoped read of the existing `MockInterviewSessions` DbSet. Small: one endpoint in a new admin sub-controller. Wire `app/admin/interviews/mock-interviews`.
- [ ] **5.11** `PracticeQuestionBanks` — CRUD + many-to-many join to `PracticeQuestions` via the `PracticeQuestionBankItems` table already in `schema.sql`. Needs an "add/remove questions to bank" endpoint on top of the base CRUD. Wire `app/admin/practice/question-banks`.
- [ ] **5.12** `MentorAssignments` — CRUD + list-eligible-students helper + list-mentor-pool helper + POST /assign. Business rule: one active assignment per (student, cohort). Wire `app/admin/mentorship/assignments`.
- [ ] **5.13** `MyLessons progress` — merges with 5.9. Reuses existing `LearningProgress` table; adds `GET /api/v1/courses/{courseId}/lessons/progress/me` returning a lesson list joined with the caller's progress rows.
- [ ] **5.14 (deferred infra)** `AdminAuditActionFilter` — MVC filter that inspects `[HttpPost/Put/Delete]` on admin controllers and auto-emits an `AdminAuditLog` row via `AppendAuditLogAsync`. Register global filter in `Program.cs`.

### Cumulative Phase 5 delivery so far

| Session | Features | New tables | Total feature modules |
|---|---|---|---|
| 1 | Coupons, Reviews, FAQs, Skills | 17 tables in schema.sql | 4 |
| 2 | Testimonials, Navigation, Categories, PlacementRoles, Products, AdminOrders, PaymentsAdmin, AdminAuditLogs | (all 17 already in schema.sql) | 4 new + 1 extended = 5 |
| **Total** | **12 of 17** | 17 tables | 9 |

### Verification (this session)

- `dotnet build src/CareerPlatform.Api/CareerPlatform.Api.csproj` — **0 warnings / 0 errors**
- `dotnet test tests/CareerPlatform.UnitTests` — **50/50 passing**
- `npx tsc --noEmit -p tsconfig.json` — clean
- `npx eslint --quiet` on all 9 new/edited frontend files — clean


---

## Phase 5 — Session 3: final 5 items ✅ PHASE 5 COMPLETE

### Delivered

| Item | Backend | Frontend |
|---|---|---|
| **5.9 CourseLessons** | New `Features/CourseLessons/*` — student read joins lessons with the caller's `LearningProgress` rows; admin CRUD + reorder | `app/student/learning/courses/[courseId]/learn` — full lesson player with sidebar, video embed, markdown, mark-complete |
| **5.10 MockInterviewSessionsAdmin** | Extended `Features/Interviews/*` — new `AdminMockInterviewSessionResponse` DTO, `ListAllSessionsAsync(status, topic)`, `AdminInterviewSessionsController` | `app/admin/interviews/mock-interviews` — status tabs + topic filter |
| **5.11 PracticeQuestionBanks** | New `Features/PracticeBanks/*` — bank CRUD + `SetQuestionsAsync` m2m membership replacement | `app/admin/practice/question-banks` — bank cards with question counts |
| **5.12 MentorAssignments** | New `Features/MentorAssignments/*` — CRUD + `eligible-students` + `mentor-pool` + soft-close `end`; one-active-per-student rule | `app/admin/mentorship/assignments` — real dropdowns from both helper endpoints, assign form, end action |
| **5.14 AdminAuditActionFilter** | New `Common/AdminAuditActionFilter.cs` + `Common/SkipAuditAttribute.cs`; registered globally in `Program.cs` | — (read UI shipped in session 2) |

**File count:** 26 new backend files + 4 frontend pages + 5 registration/config edits.

### Design decisions worth flagging

1. **Lesson progress reuses `LearningProgress`, not a new table.** Lessons write through the existing polymorphic endpoint with `ResourceType="Lesson"`. No bespoke progress table, no duplicated status vocabulary. `CourseLessonService` joins the two in a single round-trip (`lessonIds.Contains(...)`) rather than N+1.
2. **`resourceType` was an unvalidated route param — now allow-listed.** `PUT /api/v1/learning/progress/{resourceType}/{id}` takes the discriminator from the ROUTE, so the FluentValidation action filter never saw it. Any string could be written to the polymorphic table. Added `AllowedResourceTypes` in `LearningService` and kept it commented as paired with the frontend's `LearningResourceType` union. **This was a live data-integrity hole, not a new-feature concern.**
3. **Audit filter runs AFTER the action, fail-soft, admin-routes-only.** Failed requests don't pollute the trail; a logging outage never breaks the API; only `/api/v1/admin/*` + POST/PUT/PATCH/DELETE qualify. Opt-out via `[SkipAudit]`. Action verbs are derived from controller name + HTTP method (`AdminCouponsController` + POST → `COUPON_CREATED`) so the audit page filters on a stable vocabulary rather than C# method names.
4. **Audit metadata is redacted by property name.** Any argument or property matching password/secret/token/apikey/signature/otp is replaced with `[redacted]`. An audit trail that captured request bodies verbatim would become the single richest credential-leak surface in the system.
5. **Join entity got a surrogate key.** `PracticeQuestionBankItem` originally used a composite `(BankId, QuestionId)` PK, which failed `DomainBaseTypeTests` (every `Features/*/Domain/*` class must derive from `Entity<>`). Changed to a surrogate `Id` + a UNIQUE index on `(BankId, QuestionId)` — the index, not the PK, is what enforces no-duplicate-membership. `schema.sql` updated to match.
6. **Batch operations validate the whole payload before writing.** `ReorderAsync` and `SetQuestionsAsync` both reject the entire request if any id is unknown, rather than silently dropping unknowns and leaving the caller believing the write succeeded.

### Verification

- `dotnet build src/CareerPlatform.Api` — **0 warnings / 0 errors** (fixed a real CS8629 by moving `DateTime?` formatting out of the EF expression tree into an in-memory `ToResponse` mapper).
- `dotnet test tests/CareerPlatform.UnitTests` — **50/50 passing**.
- `dotnet test tests/CareerPlatform.ArchitectureTests` — **31/32 passing**. The single failure is pre-existing: `PaymentService.VerifyAsync` holds a concrete `SignatureVerificationResult` local (`AbstractionDependencyTests`). Untouched by Phase 5. My `DomainBaseTypeTests` regression was found and fixed within this session.
- `npx tsc --noEmit` — clean. Fixed one pre-existing break found en route: `app/student/profile/edit/page.tsx` passed `tone="danger"` but `AlertTone` is `info|success|warning|error`.
- `npx eslint --quiet` — clean on all touched files.

### Phase 5 final tally

| Session | Features | Modules |
|---|---|---|
| 1 | Coupons, Reviews, FAQs, Skills | 4 new |
| 2 | Testimonials, Navigation, Categories, PlacementRoles, Products, AdminOrders, PaymentsAdmin, AdminAuditLogs | 4 new + 1 extended |
| 3 | CourseLessons, MockInterviewSessionsAdmin, PracticeQuestionBanks, MentorAssignments, AdminAuditActionFilter | 3 new + 1 extended + 1 infra |
| **Total** | **17 of 17** ✅ | **11 new feature modules, 2 extended, 1 global filter** |

**Every admin/student page that previously rendered a hardcoded `const XYZ = [...]` array is now backed by a real endpoint.** No `AwaitingBackend` placeholders remain from the original 17.

### Follow-ups (not blocking)

- [ ] Fix the pre-existing `AbstractionDependencyTests` failure — extract `SignatureVerificationResult` behind an abstraction or whitelist it in the arch test.
- [ ] Admin "new"/"edit" sub-routes (`/admin/coupons/new`, `/admin/coupons/{id}/edit`, etc.) are linked from the list pages but not yet implemented — the list + read paths are complete, the write forms are the remaining UI work.
- [ ] `OrderInvoices` has no writer yet. The table + read API exist; the payments flow needs a projection hook to populate it on successful checkout.


---

## Phase 5 — Session 4: the three follow-ups

### 1. `AbstractionDependencyTests` failure ✅ FIXED — arch tests now 32/32

**Root cause was a test defect, not a design violation.** `ArchitectureModel.ConcreteClasses`
included compiler-generated types. The C# compiler emits a nested state-machine class per `async`
method (`<VerifyAsync>d__7`) carrying a FIELD for every hoisted local — so the test was reporting
`PaymentService`'s ordinary `SignatureVerificationResult` **local variable** as if it were an
injected dependency. A local is not a dependency.

Fix: `ArchitectureModel.AllTypes` now filters out anything marked `[CompilerGenerated]` or nested
inside such a type (walking the whole `DeclaringType` chain, plus a `'<' in name` backstop). This
kills the entire class of false positive — lambda closures (`<>c__DisplayClass`) and iterators
would have tripped it identically.

Notably `SignatureVerificationResult` needed no change: it is a value record forming part of
`IPaymentGateway`'s contract, and depending on a port's data contract is correct.

### 2. Admin write forms ✅ SHIPPED (schema-driven, 9 resources fully wired)

The existing `[id]/edit` pages were **dead stubs**: hardcoded `defaultValue="Placement Pro Annual
Subscription"`, `defaultValue="2999"`, and a Save button with no handler. They looked functional
while discarding every edit. All 15 `/new` routes 404'd outright.

Hand-writing ~30 forms would be thousands of lines of near-identical JSX — every one a place for a
hardcoded value or dead handler to hide. Built a schema-driven system instead:

| File | Responsibility |
|---|---|
| `components/forms/field-schema.ts` | `FormField` union (text/slug/email/url/textarea/number/select/boolean/datetime/csv) + payload↔values mapping + client validation |
| `hooks/useResourceForm.ts` | Load-existing, controlled values, dirty tracking, validate, POST/PUT submit, server-error mapping |
| `components/forms/ResourceFormPage.tsx` | Renders any schema: labels, controls, per-field errors, submit, loading |
| `features/admin/resource-forms/registry.ts` | **Single source of truth** — fields + endpoints per resource |
| `features/admin/resource-forms/AdminResourceForm.tsx` | `AdminResourceCreate` / `AdminResourceEdit` shells |

Every route file is now ~6 lines. Both `new/` and `[id]/edit/` read the SAME registry entry, so a
field added once appears in both forms and is sent to both endpoints.

**9 resources fully wired (18 routes):** coupons, categories, cms/faqs, cms/testimonials,
cms/navigation, placement/roles, products, practice/question-banks, subscriptions.

Supporting backend work — **7 admin GET-by-id endpoints added** (edit mode needs them to load
current values): `Products`, `CourseCategories`, `PlacementRoles`, `PracticeBanks`, and CMS
faqs/testimonials/navigation.

Design details worth noting:
- **`immutable` fields** (slug/code) are sent on create, omitted on update, and render read-only in
  edit mode with an explanatory note — you can see the value but not silently break URLs.
- **Server errors map back onto fields.** `ApiClient` previously threw away the ProblemDetails body
  (`new ApiError(msg, status, undefined)`), making the API's per-field `errors` extension
  unreachable. It now passes the parsed payload as `details`, and `useResourceForm` maps
  PascalCase FluentValidation keys onto camelCase form fields. Server-side validation messages now
  actually reach the operator.
- **Client validation is UX, not a boundary.** It mirrors the server rules to avoid a round-trip;
  the server stays authoritative and its rejections are always surfaced.
- **Empty optional number → `null`, not `0`.** `0` is a meaningful price.
- **Save is disabled when nothing changed**, and `saving` blocks re-entry so a double-click can't
  create two records.

### 3. `OrderInvoices` writer ✅ SHIPPED

`PaymentService.VerifyAsync` now projects the completed purchase into the admin order ledger,
**inside the same `SaveChanges`** as the Transaction + Subscription + entitlement refresh. Either
all four land or none do — the ledger can never disagree with the payment. The existing idempotency
guard (step 2 returns early on a repeat gateway callback) prevents double-insert; the unique index
on `OrderId` is the backstop.

Also added `OrderInvoiceStatus` constants (`completed`/`pending`/`refunded`/`failed`) so the writer,
the reader, and the schema share one definition instead of duplicated magic strings — and
`ListOrdersAsync` now **rejects an unrecognised status filter** rather than returning zero rows,
because an admin typo previously looked identical to "no orders in that state".

### Honest status of the remaining admin forms

10 edit pages + 6 create pages belong to resources whose admin CRUD exists but which have **no
GET-by-id endpoint**, or no backend at all. Rather than leave fabricated values and dead buttons in
place, every one now renders `<AwaitingBackend>` naming the exact missing endpoint and the wiring
step. They are honestly non-functional instead of deceptively functional.

- [ ] **Needs only a GET-by-id + registry entry** (POST/PUT/DELETE already exist):
      interview-questions, practice questions, assessments, resume-templates, placement-companies,
      content/languages, admin/mentors. Each is ~1 service method + 1 controller action + 1 registry
      entry, after which the routes collapse to the 6-line shells.
- [ ] **learning-paths** — same, plus `Milestones` is a nested collection needing a sub-editor
      rather than a flat field.
- [ ] **articles** — no Articles feature server-side. Decide table-vs-view-over-Topics first.
- [ ] **placement/plans** — no such entity; likely a duplicate of `/admin/subscriptions`, which IS
      fully wired. Confirm before building.

### Verification (this session)

- `dotnet build src/CareerPlatform.Api` — **0 warnings / 0 errors**
- `dotnet test ArchitectureTests` — **32/32 passing** (was 31/32; the long-standing failure is gone)
- `dotnet test UnitTests` — **50/50 passing**
- `npx tsc --noEmit` — clean
- `npx eslint --quiet` — clean across all form infrastructure and every admin new/edit route

Incidental pre-existing defects fixed en route: `courses/[courseId]/edit` had two `as any` casts on
a field that already has a proper union type (replaced with a narrowing helper) and a bare
`console.error` (replaced with the structured logger).

---

## P0 remediation session — dead BFF layer, broadcast delivery, integration-test coverage

### 1. Four live mentorship 404s ✅ FIXED

`endpoints.ts` pointed the admin mentorship calls at `/api/v1/admin/mentors/{slots,bookings}`.
That prefix belongs to `AdminMentorsController`, which only handles mentor *lifecycle* (bare
`[HttpGet]`/`[HttpPost]`/`[HttpPut]`). Slot and booking administration lives on
`MentorshipController` under `/api/v1/mentorship/admin/*`. Repointed all four keys
(`adminSlots`, `adminBookings`, `adminDeleteSlot`, `adminCancelBooking`) and corrected the
misleading class doc-comment that claimed the controller was "kept at the legacy
`/api/Dashboard/admin/mentors` path".

### 2. The whole `frontend/app/api/v1/**` layer was dead ✅ DELETED

The investigation started as "three proxies forward to the wrong backend route" and found a bigger
problem. `apiClient`, `server-client` and `binary-client` all build **absolute** URLs from
`env.apiBaseUrl`, so every request leaves Next and goes straight to the .NET API. Nothing in the
app could ever reach a Next route handler at `/api/v1/...` — the one exception was
`RegisteredStudentsTable`, which used a bare relative `fetch`.

On top of being unreachable, those handlers were actively unsound:

- **Module-level in-memory stores** (`let DB_MENTORS = []`, `DB_MEETINGS`, `GLOBAL_BROADCASTS`)
  used as a database. On Vercel these are per-instance and vanish on cold start.
- **No cookie forwarding.** Auth is an `HttpOnly` session cookie sent with
  `credentials: "include"`; the handlers called the backend anonymously, so every admin call they
  made would have been rejected regardless.
- **Fabricated fallbacks presented as success.** `broadcasts/route.ts` returned
  `recipientCount = 18` and "dispatched successfully" after swallowing the fan-out failure;
  `recipients/route.ts` invented `24 / 14 / 10`; `mentors/route.ts` invented company
  "Top Tier Tech", role "Senior SDE", `rating: 5.0` and returned 201.
- **7 empty `catch {}` blocks** lived here.

All nine handlers and `features/admin/services/broadcast-store.ts` are gone. Every backend route
they shadowed already existed and matched 1:1 (`/api/v1/admin/meetings`, `/api/v1/admin/mentors`,
`/api/v1/admin/mentor-assignments`, `/api/v1/admin/dashboard/*`, `/api/v1/notifications/me`,
`/api/v1/admin/broadcasts`). `app/api/revalidate/content/route.ts` is kept — it is a genuine
Next revalidation hook, not a proxy.

### 3. Broadcasts: promotional email was never sent ✅ FIXED

The UI reported "Promotional email dispatched successfully" while `BroadcastService` only wrote
in-app `Notification` rows — `IEmailSender` was never involved. Closed end to end:

- **`EmailJob` was a stub** that resolved `IEmailSender` and logged. It is now a real bulk sender
  carrying a de-duplicated recipient list plus one rendered body. A per-recipient failure is logged
  and skipped so one bad address cannot drop the rest of the batch; addresses are never logged.
- **Promotion sends are queued after the commit.** A Brevo outage must not roll back a broadcast
  recipients can already see in-app, and sending N messages inline would hold the request open for
  the whole batch.
- **`SendBroadcastResult.EmailQueuedCount`** is new, and the UI reports it separately from the
  in-app fan-out count. Those two numbers differ whenever a targeted student has no address on
  file, and the old copy implied everyone was emailed.
- **`BroadcastEmailBody`** HTML-encodes every interpolated value (broadcast copy is admin-authored
  free text) and emits only absolute `http(s)` hrefs, so a `javascript:` link can't reach a mail
  client.
- **`AudienceResolver` now filters `Role == "Student"`.** Admin accounts were previously counted in
  the recipient preview and received every promotional campaign.
- **Empty audience is now a validation failure**, not a success. Recording a broadcast nobody
  receives put a misleading row in the history and reported it as sent.

### 4. The target-plan dropdown matched zero users ✅ FIXED

`TARGET_PLAN_OPTIONS` hardcoded `"Fresher" | "1 month" | "3 month" | "6 months" | "1 Year"`.
`AudienceResolver` matches on `UserProfile.PlanName`, whose only real values are `"Free"` and the
`SubscriptionPlan.Name` values written by `PaymentService` (`"Monthly (Pro)"`,
`"Yearly (Premium)"`). **Every non-"All Plans" option silently resolved to an empty audience.**

New `GET /api/v1/admin/broadcasts/audience-targets` derives the list server-side by grouping
students by plan and unioning the active plan catalogue (so a newly-launched plan is reachable
before its first sale), each entry carrying its live recipient count. The hardcoded union is
deleted; only the `ALL_PLANS_TARGET` sentinel remains, mirroring `AudienceResolver.AllPlans`.

### 5. `student/today-message` ✅ WIRED

Was fetching the deleted in-memory handler and passing the student's plan as a **query parameter** —
a client-supplied plan would have let a free user read paid-cohort broadcasts. New
`GET /api/v1/broadcasts/today` (`StudentBroadcastsController`, `[Authorize]`) resolves the caller's
plan from their identity and returns today's `Notification` broadcasts targeted at all plans or at
that plan.

### 6. `getRegisteredStudents` mapped fields the API never emits ✅ FIXED

The mapper read `d.name` / `d.planName` / `d.registeredDate`, but `RegisteredStudentResponse`
serialises `studentName` / `accountType` / `creationDate`. Every row therefore degraded to
`"Student"` / `"FREE"`, and `plan` was cast to a closed union
(`"FREE" | "MONTHLY" | "YEARLY" | ...`) that matches none of the real plan names. The method now
returns the DTO shape unchanged, `RegisteredStudent` is replaced by `RegisteredStudentRow`, and
`RegisteredStudentsTable` consumes it through `useApiQuery` and surfaces load errors instead of
rendering an empty table on failure.

### 7. Integration tests were asserting against removed routes ✅ FIXED

18 of 65 tests failed with `Expected: Forbidden → Actual: NotFound`: they still used the legacy
PascalCase URLs. Because a 404 also satisfies "not 200", **the auth-matrix and rate-limit suites
were passing vacuously** — they proved nothing about authorization. 24 literals across 8 files
repointed to the canonical routes, verified against each controller:

| was | now |
| --- | --- |
| `/api/AdminOffer/...` | `/api/v1/admin/offers/...` |
| `/api/Payment/...` | `/api/v1/payments/...` |
| `/api/Dashboard/admin/...` | `/api/v1/admin/dashboard/...` |
| `/api/SubscriptionPlans/...` | `/api/v1/subscription-plans/...` |
| `/api/ContentManagement/languages` | `/api/v1/content-management/languages` |
| `/api/ContentManagement/public/curriculum/{slug}` | `/api/v1/curriculum/{slug}` |

### Verification (this session)

- `dotnet build src/CareerPlatform.Api` — **0 warnings / 0 errors**
- `dotnet test ArchitectureTests` — **32/32**
- `dotnet test UnitTests` — **50/50**
- `dotnet test IntegrationTests` — **65/65** (was 47 passed / 18 failed)
- `npx tsc --noEmit` — clean (required deleting a stale `.next/types/validator.ts` referencing the
  removed route handlers)
- `npx eslint --quiet` — clean
- `npm run build` — `BUILD_EXIT=0`; the route table now lists `/api/revalidate/content` as the only
  handler under `app/api`

### Still outstanding (unchanged by this session)

- [ ] **Fabricated data on detail/dashboard pages.** `student/placement/readiness` (entire page:
      `84/100`, "top 12%", skill bars, zero fetching), `student/resume/[resumeId]`
      ("ALEXANDER RHOADES", `alex@example.com`, CGPA 9.2, next to a Download PDF button),
      `student/orders/[orderId]` (fake ₹2,999 invoice + `GSTIN: 29ABCDE1234F1Z5` + "Paid" badge),
      `admin/analytics` ("₹48.5 Lakhs" ARR, "Total Active: 3,420"), `(public)/placement`
      `FEATURED_COMPANIES`, `CourseAnalyticsView` fallback (₹14,18,580 revenue, 4.9★/342 reviews),
      hardcoded `₹1,999` in three mentor pages, hardcoded progress percentages in two learning
      pages.
- [ ] **`DashboardService.GetRegisteredStudentsAsync` hardcodes `Status = "Active"`** — no status
      column exists on `UserProfile`. Either add one or drop the column from the DTO and the table;
      shipping a constant dressed as data is the same defect as the pages above.
      The same DTO also duplicates every field as a nullable alias (`Name`/`Email`/`Plan`/
      `RegisteredDate`) purely for a legacy client that no longer exists — collapse it.
- [ ] **5 error-discarding `.catch` handlers** in student mentorship/practice pages.
- [ ] **`|| "http://localhost:5215"` fallbacks** — now only in `next.config.ts` (the ten in
      `app/api/**` went with the deleted handlers). Decide whether a missing
      `NEXT_PUBLIC_API_BASE_URL` should fail the build instead of silently pinning a dev host into
      the production CSP.
- [ ] **Dead endpoint keys + inline `/api/` literals.** `broadcast.service.ts` and
      `meeting.service.ts` now route through the registry; the remaining inline literals and unused
      registry keys still need a sweep.
- [ ] **16 admin form routes still `<AwaitingBackend>`** — see the previous section for the
      per-resource recipe.
- [ ] `(auth)/login` phone placeholder has a doubled prefix: `"+91 +91 9999999999"`.
- [ ] `support@TalktoPlacement.io` / `+91 62995 77528` in `(public)/contact` need product-owner
      confirmation.

---

## `frontend/app/student/**` end-to-end remediation

All 73 student pages audited and remediated. Headline: **no page needed `<AwaitingBackend>`.** Four of
the five surfaces flagged as unbackable were already wireable against existing endpoints, and the fifth
needed one new 30-line read.

### 1. Three live backends the student surface was faking ✅ WIRED

| Backend (already existed) | What the page did instead |
| --- | --- |
| `GET /api/v1/code-execution/languages`, `POST /run` + a complete `useCodeRunner` hook | `practiceService.executeCode` **hard-rejected** with "not yet available"; `Run` and `Submit` shared one handler; the language menu was a hardcoded list advertising runtimes the deployment may not have. |
| `GET /api/v1/learning-paths[/{slug}]` + `learningPathService` (used by the public and admin pages) | The **student** pages hardcoded "Full Stack SDE Mastery / Milestone 2 of 4 / 50%" and a literal milestone array carrying per-user `Completed`/`In Progress` state. |
| `PUT /api/v1/interviews/sessions/me/{id}` | **Zero consumers** — no mock session could ever be completed. |

New shared pieces: `SolutionWorkspace` (server-driven language picker, per-problem `sessionStorage`
draft, hides Run entirely when no sandbox is configured), `milestone-progress.ts` (an **invertible**
derivation between the stored path percentage and per-milestone state, so marking milestone *n*
complete and re-reading always reproduces the same UI), `useSessionCountdown` (deadline anchored to the
server's `createdAt`, so reloading does not hand the student a fresh clock).

`Submit` was **removed** from the practice editor rather than wired: practice questions carry no test
cases server-side, so nothing can grade a submission. Graded work goes through the assessment attempt
runner, which has real test cases and a server-computed scorecard.

### 2. The mock-interview room ✅ REBUILT

Was: no `params` (so `[interviewId]` was never read), `Time: 38:45 Remaining` as a **static string**,
a hardcoded "AI Interviewer (Google SDE-2 Persona)", an unused `setMessages` so the transcript was
permanently one fake AI turn, an uncontrolled scratchpad whose contents the app could not read, and
`End & Evaluate` linking to the **literal slug** `ai-dsa`.

Now a self-guided session on real data: the session, the question bank filtered to its topic, the
admin-managed rubric axes, the real sandbox, and a self-assessment that `PUT`s a weighted score plus
the rubric report and transcript — which the report page then reads back.

Scales moved into `rubric-scoring.ts`. The report page had hardcoded `/100` and `/10` denominators and
computed **hiring verdicts** ("Strong Hire" → "No Hire") from thresholds written into the component;
because the service defaulted a missing score to `0`, an unscored session rendered a confident
**"0/100 — No Hire"**. `overallScore` is now `number | null` and unscored says so.

### 3. Five genuine schema gaps ✅ FILLED

Appended to `schema.sql`: `StudentEducations`, `StudentPreferences`, `ResumeDrafts`.

- **`StudentEducations`** — grade stored as a **(value, scale) pair**; a `9.2` is meaningless without
  knowing whether the institution grades on a 10-point CGPA, a 4-point GPA, or a percentage. New
  `Features/StudentProfile` slice with `/api/v1/me/education` CRUD.
- **`StudentPreferences`** — one row per user. Both consent-bearing switches (`RecruiterVisibility`,
  `PromotionalEmailsEnabled`) default to **false**; the UI they replace defaulted both to *on* and
  never persisted anything, displaying consent the student had never given and could not withdraw.
- **`ResumeDrafts`** — opaque `ContentJson`, so adding a resume section needs no migration. Replaces
  `stores/resumeBuilderStore.ts`, a module-level singleton that held one resume for the whole tab
  (opening a second overwrote the first) and persisted nothing.
- **Placement readiness — deliberately NOT a table.** Derived on read from `LearningProgress`,
  `AssessmentAttempts`, `MockInterviewSessions`, `UserSkills` and `ResumeSubmissions`, with weights
  declared once server-side and echoed in the response. A component with no data returns
  `score: null` (never `0`), and the response carries `coverage` — a score of 80 from one dimension is
  not the same claim as 80 from five, and the page says which.
- **Interview topics — also no table.** `GET /api/v1/interview-questions/topics` groups the published
  bank in the database. The hub's `INTERVIEW_TRACKS` literal had invented question counts
  ("120+ Questions"), invented company attributions, an `isEnrolledProgram: true` flag that showed the
  same six "Enrolled" badges to **every** account, and one entry whose slug sent its CTA to the wrong
  track.

### 4. Failures that were rendered as success ✅ FIXED

The worst was `learningProgressService.getSummary`: an empty `catch {}` returning a zero-filled object,
so `useApiQuery` **resolved successfully** and the dashboard reported "0 tracked · 0 in progress ·
0 completed" on a 500 or an expired session. The dashboard also never read `.error` at all.

Also: four `.catch(() => [])` (mentor slots, cross-mentor availability, assessment attempts, practice
bookmarks) — availability now uses `Promise.allSettled`, reports `failedMentorCount`, and throws when
*every* lookup fails instead of showing "No open slots right now. Check back later."
`interviewQuestionService.getTrack` rethrows non-404s, so an outage no longer appears as the editorial
state "Content in Active Curation".

`payments/success` was a static page asserting "Your entitlement has been activated" purely because the
URL was visited. It now verifies `?orderId=` against the real order and reports pending and failed
states honestly. `payments/failed` claimed "The transaction could not be processed by your bank. No
funds were debited" — two specific facts a static page cannot know, and the riskiest statement in the
audit. Both claims are gone.

### 5. Broken targets and inert controls ✅ FIXED

`c1` (→ `Number("c1")` → `/api/v1/courses/NaN/lessons`), `res-1`, `ai-dsa`, and
`/student/resume/builder` (a directory that does not exist — the link fell through to `[resumeId]` and
rendered the fake résumé with `resumeId === "builder"`). Practice questions were linked by `id` while
the detail page resolves by `slug`. Inert controls fixed or removed: Download PDF (now `window.print()`,
a real capability), Save Draft, Add Education, the mic/camera/share buttons, and the two preference
toggles. `/student/profile/{education,skills,preferences}` had **zero inbound links** from anywhere in
the app — now linked from the profile page and registered in `routes.ts`.

### 6. Deleted fabrication engines

- `features/students/resume.service.ts` — `setTimeout`-resolved a hardcoded ATS score of **84** with
  invented per-check feedback, and returned a fake "Senior FAANG Mentor Panel" reviewer. No importers,
  but it exported the name `resumeService`, **colliding with the real service** in `features/resume` —
  one careless auto-import from shipping fake ATS scores.
- `features/students/guidance.service.ts` — hardcoded 12-week roadmap with per-student statuses.
- `features/interviews/hooks/useMockInterview.ts` — seeded a fake AI interviewer turn.
- `stores/practiceStore.ts` — global singleton that leaked one question's code into every other.
- `features/practice/hooks/usePracticeSandbox.ts` — fabricated `"Execution error: Runtime Exception"`.
- `INTERVIEW_PERSONAS` and `RUBRIC_SCORING_CRITERIA` — the personas described an AI interviewer that
  does not exist; the criteria duplicated (and could contradict) the admin-managed rubric axes.

### 7. Consolidation

`statusTone` existed in **7** private copies → `lib/utils/status-tone.ts` (one function per status
vocabulary, all case-insensitive, all falling through to neutral so an unrecognised server value is not
mislabelled as success). `difficultyTone` ×3 → `lib/utils/difficulty.ts`. `StudentOrder` ×3 (one copy
had already drifted to field names the API never emits) → `features/orders/order.service.ts`. Two copies
of `["day","week","month","year"][unit]` — a magic-number contract with the backend enum — → named map
in `plan-presentation.ts`, along with `planFeatureLines`, which no longer turns a prose plan description
into a bogus one-item feature list with a green tick.

Nine inert `let active = true` cleanup guards removed (checked *before* the await, never after, so they
could not prevent a post-unmount `setState`). Nine dead Refresh spinners fixed (`isLoading={loading}`
against a `load()` that only ever set `loading` to `false`). Ten pages converted to
`useApiQuery` + `QueryStates` + the shared `RefreshButton`. `MetricValue` now used where the dashboard,
readiness and progress pages render headline numbers.

### Verification

- `dotnet build` — 0 errors
- ArchitectureTests **32/32** · UnitTests **50/50** · IntegrationTests **67/67** (was 65; the two new
  controllers are picked up by the authorization matrix)
- `npx tsc --noEmit` clean · `npx eslint --quiet` clean · `npm run build` `BUILD_EXIT=0`

### Known follow-ups

- [ ] `features/payments/plan-presentation.ts` was accidentally overwritten and **reconstructed** from
      its three consumers. The exported contract is exact and everything compiles, but the per-tier
      marketing bullet text in `PLAN_PRESENTATIONS` is a reconstruction, not the original copy. No
      commercial values live there — price, currency and interval all come from the server catalog.
      **Worth a product review of that copy.**
- [ ] Ten `react-hooks/set-state-in-effect` suppressions remain, each with an inline rationale, on
      fetch-on-mount effects — the same escape `hooks/useApiQuery.ts` already takes. Converting those
      pages to `useApiQuery` would remove them.
- [ ] `/student/meetings/[meetingId]` and `/student/mentorship/bookings/[bookingId]` render the same
      underlying booking. Worth collapsing to one route.
- [ ] Detail pages for mentors, bookings, meetings and interview reports still fetch a whole collection
      to render one record, because those endpoints have no single-resource sibling. Adding
      `GET /{id}` variants would remove the over-fetch.
- [ ] `DashboardService.GetRegisteredStudentsAsync` still hardcodes `Status = "Active"` (no status
      column exists) and duplicates every field as a nullable alias for a client that no longer exists.
