# Release history

## arch-v2 — 2026-08-16

Modular Monolith + Package-by-Feature + MVC controllers. Single canonical `/api/v1/{kebab-case}` HTTP surface. FluentValidation runs via a global MVC action filter; MediatR fully removed. Domain events dispatched by an in-process `IDomainEventDispatcher` with a plain `IDomainEventHandler<TEvent>` contract. Frontend rewritten to the same `/api/v1/*` surface; Next.js internal API routes relocated to match.

### Highlights

- 25 features migrated to `Features/<Feature>/{Controller,Service,Dto,Validation,Domain}/<Feature>Module.cs`.
- Legacy dual `[Route(...)]` per controller collapsed to a single canonical `[Route("api/v1/…")]`.
- `Common/IEndpoint.cs`, `Infrastructure/EndpointRegistration.cs`, `Middleware/ApiVersionRewriteMiddleware.cs`, `Common/ValidationBehavior.cs`, `Interceptors/MediatrDomainEventDispatcher.cs`, and MediatR-shaped arch tests deleted.
- Global `Common/FluentValidationActionFilter` wires validators onto MVC action arguments.
- OpenAPI document publishes the routes as-declared — no post-hoc path rewriting.
- Frontend `endpoints.ts` registry and every service caller use `/api/v1/…`; Next.js internal API route directories renamed to match (`app/api/Dashboard/…` → `app/api/v1/…`).
- Arch tests updated to the new shape: `ControllerLocationTests`, rewritten `SliceStructureTests` (layered-folder + module check), `AbstractionDependencyTests` retargeted to `ServiceTypes`, obsolete `HandlerConventionTests` / `ResultReturnTypeTests` / `ValidatorConventionTests` deleted.

### Verification

- Backend: `dotnet build CareerPlatform.sln -c Debug` — 0 warnings, 0 errors.
- Frontend: `npm run build` — 136/136 pages generated, TypeScript clean.
- Smoke: `node backend/scripts/swagger-smoke.mjs` against a running host classifies every route from `/swagger/v1/swagger.json` as PASS/FAIL.
- Mobile: `backend/scripts/generate-mobile-client.sh <kotlin|swift5|dart|typescript-fetch> <out-dir>` regenerates a typed client from the same OpenAPI doc.

### Files describing this release

- `backend/VERSION` — the marker: `arch-v2`.
- `docs/architecture/structure.md` — target architecture (§7 lists the shipped arch tests).
- `docs/architecture/requirement.md` — motivations.
- `docs/architecture/tasks.md` — session-by-session task log.
- `docs/api/mobile-client-generation.md` — how to regenerate a mobile client.
