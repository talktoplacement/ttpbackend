# Mobile integration guide

Everything the CareerPlatform web app does goes through the same REST API a native mobile
client uses. This doc tells the mobile team what they need to know **beyond** the auto-generated
[API reference](./README.md).

## 1. Base URLs

| Environment | Base URL |
|---|---|
| Local dev | `http://localhost:5215` (backend runs on Kestrel) |
| Staging | *set per environment; e.g. `https://api.staging.placementpro.io`* |
| Production | *set per environment; e.g. `https://api.placementpro.io`* |

The mobile app should read this from a build-time constant so debug/staging/prod builds hit the
right host.

## 2. Authentication

All non-`AllowAnonymous` endpoints require an HTTP Bearer JWT:

```
Authorization: Bearer <accessToken>
```

### 2.1 Getting a token

There are two supported issuers, both accepted by the same `JwtBearer` middleware:

**a) First-party login** — for email/password students, mentors, and admins.

```http
POST /api/Auth/login
Content-Type: application/json

{ "email": "student@example.com", "password": "…" }
```

Response body:
```json
{
  "accessToken": "eyJhbGciOi…",
  "tokenType": "Bearer",
  "expiresAtUtc": "2026-08-31T12:34:56Z",
  "user": { "id": "…", "email": "…", "role": "Student", "fullName": "…" }
}
```

**b) Supabase-issued JWT** — if the mobile app uses the Supabase SDK for auth, its access token
is already accepted by the backend (same signing key, same issuer). No extra step needed.

### 2.2 Storing the token

- **iOS**: Keychain (`kSecClassGenericPassword`), never `UserDefaults`.
- **Android**: `EncryptedSharedPreferences` or the Jetpack Security library, never plain
  `SharedPreferences`.
- Both platforms: never log the token, never write it to a crash-reporting payload.

### 2.3 Refresh

Access tokens live 15 minutes by default. When a request returns `401`, the app should
re-invoke `POST /api/Auth/login`. A dedicated refresh endpoint is on the backend roadmap; when
it lands, the collection here will pick it up automatically.

## 3. Errors

Every 4xx/5xx returns [RFC 7807 problem+json](https://datatracker.ietf.org/doc/html/rfc7807):

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "Session 42 was not found.",
  "code": "Interview.SessionNotFound"
}
```

- `title` — short reason phrase; safe to display when no `detail` is present.
- `detail` — human-readable message; safe to show to end users.
- `code` — stable machine-readable code (`Feature.ErrorType`); use for localisation.
- `errors` (400 only) — validation failures keyed by field name.

**Rule for the mobile app**: log `code`, surface `detail` (or `title` fallback) to the user.

## 4. File uploads

Two endpoints today accept files:

- `POST /api/Resumes/me/upload-pdf` — student resume, PDF only, 1 MB max
- `POST /api/Admin/ContentManagement/import/interview` — admin xlsx, 8 MB max

Both use `multipart/form-data` with a single field named `file`:

```http
POST /api/Resumes/me/upload-pdf HTTP/1.1
Authorization: Bearer <token>
Content-Type: multipart/form-data; boundary=----FormBoundary…

------FormBoundary…
Content-Disposition: form-data; name="file"; filename="resume.pdf"
Content-Type: application/pdf

<binary>
------FormBoundary…--
```

- iOS: `URLSession` with a manually constructed multipart body, or Alamofire's
  `MultipartFormData`.
- Android: OkHttp/Retrofit `MultipartBody.Builder` with a `Part.createFormData("file", …)`.
- React Native: `FormData` — same API as the browser.

## 5. File downloads

`GET /api/Certificates/{id}/download`, `GET /api/Resumes/…/download`, etc. all return the raw
bytes with `Content-Type: application/pdf` (or the actual mime type) and a
`Content-Disposition: attachment; filename=…` header.

- iOS: `URLSession.downloadTask` writes straight to disk.
- Android: OkHttp with `.body().byteStream()` or a `WorkManager` job for large files.

## 6. CORS + CSRF

- **CORS**: only applies to browsers. Ignore.
- **CSRF**: the backend does NOT enforce anti-forgery tokens. The `X-CSRF-Token` header you'll
  see the web frontend send is a defence-in-depth artefact that mobile clients can and should
  skip entirely.

## 7. Rate limiting

Sensitive routes (all state-changing endpoints + all `/api/Admin/*` and `/api/Mentor/*` routes)
are partitioned by user id (or client IP if anonymous). If you exceed the limit you'll get:

```
HTTP/1.1 429 Too Many Requests
Retry-After: 60
```

Back off using the `Retry-After` value.

## 8. Pagination

None of the current endpoints paginate. All list endpoints return the full result set.
Pagination will be added incrementally; when it lands, the response body will change from a
plain `[…]` array to `{ items: […], nextCursor: "…" }`. Design mobile screens to consume both
shapes (feature-flag on the client, or check `Array.isArray(response)`).

## 9. Versioning

The API is unversioned today; routes are stable. Breaking changes will be announced in the
release notes and — where required — introduced under a new path (`/api/v2/…`). Additive
changes (new endpoints, new optional fields on existing responses) are made in place.

## 10. Testing

Import [`postman-collection.json`](./postman-collection.json) into Postman or Insomnia. Set the
collection variables:

- `baseUrl` — e.g. `http://localhost:5215`
- `token` — paste the JWT you get from `POST /api/Auth/login`

Every request then runs authenticated against your backend.

## 11. Known gaps for mobile

Things the web app can do today that the mobile app **cannot** yet without more backend work:

1. **Payment checkout redirect** — Razorpay flow uses a web SDK. The `POST /api/Payment/create-order`
   endpoint returns an order id you can hand to the Razorpay mobile SDK, but the web-only
   redirect handler is not reusable. Mobile devs should use Razorpay's official iOS/Android SDK
   with the returned `orderId`, then call `POST /api/Payment/verify` to reconcile.
2. **Code execution sandbox** — `practiceService.executeCode` returns a "not available" error
   on the web and will do the same on mobile. Container-isolated runner is on the roadmap.
3. **ATS analysis on resumes** — same "not available" state.

Everything else in the endpoint reference is stable, mobile-friendly, and covered by the
Postman collection.
