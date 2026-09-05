# Mobile client generation

The backend publishes an OpenAPI 3 document at `/swagger/v1/swagger.json`. Every mobile client is generated from that single source of truth, so the client's types and method signatures always match the server's controller contracts.

## URL surface

- Base URL: `https://<host>/api/v1`
- Naming: kebab-case, plural nouns (`/api/v1/resume-templates`, `/api/v1/student-resumes`).
- Auth: `Authorization: Bearer <supabase-jwt>` on every non-public endpoint.
- Content type: `application/json` for JSON payloads, `multipart/form-data` for the two upload endpoints (resume PDF, interview-question .xlsx importer).
- Error envelope: RFC 7807 ProblemDetails; per-field validation failures live under the `errors` extension.

## Prerequisites

Install [`openapi-generator-cli`](https://openapi-generator.tech/) once. Either:

```
npm i -g @openapitools/openapi-generator-cli
```

or

```
brew install openapi-generator
```

The generator downloads the correct Java runtime on first use — no separate JDK install needed.

## Generate

Run against a live backend (local or shared dev):

```
# Android / KMP (OkHttp + Moshi)
BASE_URL=http://localhost:5215 backend/scripts/generate-mobile-client.sh kotlin ./out/kotlin

# iOS / macOS (URLSession + async/await)
BASE_URL=http://localhost:5215 backend/scripts/generate-mobile-client.sh swift5 ./out/swift

# Flutter (dio)
BASE_URL=http://localhost:5215 backend/scripts/generate-mobile-client.sh dart ./out/dart

# Web / React Native (native fetch, TypeScript ≥ 3.7)
BASE_URL=http://localhost:5215 backend/scripts/generate-mobile-client.sh typescript-fetch ./out/ts
```

The generator writes a full client library (models + APIs + a configured `ApiClient`) into the output directory. Each API surface class corresponds to one backend controller; each method corresponds to one `[HttpGet]` / `[HttpPost]` / `[HttpPut]` / `[HttpDelete]` action.

## Auth wiring

Every generated client exposes a place to inject the Supabase-issued JWT.

Kotlin:

```kotlin
val client = ApiClient(basePath = "https://api.example.dev/api/v1")
client.setBearerToken(supabaseSession.accessToken)
```

Swift:

```swift
let config = ApiConfiguration(basePath: "https://api.example.dev/api/v1")
config.accessToken = supabaseSession.accessToken
```

Dart:

```dart
final client = ApiClient(basePath: "https://api.example.dev/api/v1");
client.addDefaultHeader("Authorization", "Bearer $accessToken");
```

TypeScript:

```ts
const config = new Configuration({
  basePath: "https://api.example.dev/api/v1",
  accessToken: async () => supabase.auth.getSession()
    .then(s => s.data.session?.access_token ?? ""),
});
```

## Verification

After regenerating, run the smoke script against the same host to confirm every route on the generated client is reachable:

```
BASE_URL=http://localhost:5215 TOKEN=<jwt> node backend/scripts/swagger-smoke.mjs
```

A green run means every method the generator emitted corresponds to a real controller action.

## Versioning contract

`v1` is the sole public version. Breaking changes cut a new spec at `/api/v2/*`, and the generated `v1` client keeps working until `v1` is retired. Non-breaking additions (new fields, new endpoints) reissue `v1`; regenerate the client to pick them up.
