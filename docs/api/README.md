# CareerPlatform HTTP API — reference for client integrators

This is the authoritative reference for wiring **any** client — web, mobile, third-party — against the CareerPlatform backend. Read this before writing an HTTP call.

## 1. Base URL

    https://<host>/api/v1/{resource}

- `v1` is the URL-segment API version. Bump to `v2` only for **breaking changes**; additive changes ship inside `v1`.
- The same request also works against `/api/{resource}` (unversioned, legacy path used by the web frontend). New integrations must use `/api/v1/…` — the server pins that URL scheme.
- Every response carries `X-Api-Version: v1` so a client can confirm which version served it, plus `X-Correlation-Id: …` for tracing.

## 2. Resource naming

| Rule | Example |
| --- | --- |
| **Lowercase** | `courses`, not `Courses` |
| **Kebab-case** for multi-word resources | `subscription-plans`, `learning-paths` |
| **Plural nouns** for collections | `mentors`, not `mentor` |
| **Singular** for a singleton owned by the caller | `/api/v1/me` |
| RPC-style actions are **suffixes** on the parent resource | `POST /api/v1/notifications/read-all` |

Route matching is case-insensitive, so a client typing `/api/v1/Courses` still resolves — but treat the lowercase, kebab form as the canonical mobile URL.

## 3. HTTP methods

| Method | Purpose | Present? |
| --- | --- | --- |
| `GET` | Read a resource or collection | ✅ 68 endpoints |
| `POST` | Create a resource or trigger an RPC-style action | ✅ 43 endpoints |
| `PUT` | Full-resource update (send the entire object) | ✅ 25 endpoints |
| `DELETE` | Remove a resource | ✅ 15 endpoints |
| `PATCH` | Partial update | ❌ Not used — all updates are full-resource `PUT` |

**Design note on `PATCH`:** the API standardizes on `PUT` for updates. When you edit one field, send the entire resource on `PUT`. This keeps request bodies self-describing and idempotent, and avoids JSON-Patch/JSON-Merge-Patch ambiguity for mobile clients.

## 4. Authentication

- Every endpoint except `/api/v1/auth/*`, `/health/live`, and `/health/ready` requires authentication.
- Send the Supabase-issued JWT as `Authorization: Bearer <token>`.
- Role-scoped endpoints (`RequireAuthorization("Admin" | "Mentor")`) 403 for the wrong role.
- Failure → RFC 7807 problem+json (`type`, `title`, `status`, `detail`).

## 5. Error envelope

Non-2xx responses use RFC 7807 `application/problem+json`. Validation failures carry a `errors` field mapping field names to messages. Rate limits (429) include `Retry-After: <seconds>`. Correlation id is always echoed on `X-Correlation-Id`.

## 6. Live contract — OpenAPI / Swagger

- Interactive UI: **`/swagger`**
- Machine-readable JSON: **`/swagger/v1/swagger.json`**
- Use it to generate a typed mobile client (e.g. `openapi-generator generate -g swift5|kotlin -i /swagger/v1/swagger.json`).
- The Swagger doc shows paths in canonical `/api/v1/kebab-case` form — that is the URL your client should call.

## 7. Endpoint map (v1)

Paths below use the canonical mobile URL scheme. Auth column: `pub` = public, `auth` = any authenticated user, `student` / `mentor` / `admin` = role-scoped.

### Auth
| Method | Path | Auth |
| --- | --- | --- |
| POST | `/api/v1/auth/login` | pub |
| POST | `/api/v1/auth/register/start` | pub |
| POST | `/api/v1/auth/register/verify` | pub |
| POST | `/api/v1/auth/register/resend` | pub |
| POST | `/api/v1/auth/password/forgot` | pub |
| POST | `/api/v1/auth/password/reset` | pub |
| POST | `/api/v1/auth/sync` | auth |

### Me
| Method | Path | Auth |
| --- | --- | --- |
| GET | `/api/v1/me` | auth |
| PUT | `/api/v1/me` | auth |
| POST | `/api/v1/me/password` | auth |

### Courses & Plans
| Method | Path | Auth |
| --- | --- | --- |
| GET | `/api/v1/courses` | pub |
| GET | `/api/v1/courses/{slug}` | pub |
| POST | `/api/v1/courses` | admin |
| PUT | `/api/v1/courses/{id}` | admin |
| DELETE | `/api/v1/courses/{id}` | admin |
| GET | `/api/v1/subscription-plans` | admin |
| GET | `/api/v1/subscription-plans/catalog` | pub |
| GET | `/api/v1/subscription-plans/entitlement` | auth |
| POST | `/api/v1/subscription-plans` | admin |
| PUT | `/api/v1/subscription-plans/{id}` | admin |
| PUT | `/api/v1/subscription-plans/{id}/active` | admin |
| DELETE | `/api/v1/subscription-plans/{id}` | admin |

### Content (learning catalog)
| Method | Path | Auth |
| --- | --- | --- |
| GET | `/api/v1/content-management/languages` | pub |
| GET | `/api/v1/content-management/sections` | pub |
| GET | `/api/v1/content-management/topics` | pub |
| POST/PUT/DELETE | `/api/v1/content-management/…` | admin |
| GET | `/api/v1/content-management/public/curriculum/{lang}` | pub |

### Learning progression, assessments, practice, interviews
| Method | Path | Auth |
| --- | --- | --- |
| GET | `/api/v1/learning-paths` | pub |
| GET | `/api/v1/learning-paths/{slug}` | pub |
| GET | `/api/v1/assessments` | pub |
| GET | `/api/v1/practice` | pub |
| GET | `/api/v1/interview-questions` | pub |
| GET | `/api/v1/interviews/sessions/me` | auth |
| POST | `/api/v1/interviews/sessions/me` | auth |
| PUT | `/api/v1/interviews/sessions/me/{id}` | auth |

### Mentors & Mentorship
| Method | Path | Auth |
| --- | --- | --- |
| GET | `/api/v1/mentorship/mentors` | auth |
| GET | `/api/v1/mentorship/mentors/{mentorId}/slots` | auth |
| POST | `/api/v1/mentorship/book` | auth |
| GET | `/api/v1/mentorship/my-bookings` | student |
| GET | `/api/v1/mentor/profile` | mentor |
| PUT | `/api/v1/mentor/profile` | mentor |
| GET | `/api/v1/mentor/overview` | mentor |
| GET | `/api/v1/mentor/sessions` | mentor |
| GET | `/api/v1/mentor/students` | mentor |
| GET | `/api/v1/mentor/students/{studentUserId}` | mentor |
| GET | `/api/v1/mentor/slots` | mentor |
| POST | `/api/v1/mentor/slots` | mentor |
| DELETE | `/api/v1/mentor/slots/{id}` | mentor |
| GET | `/api/v1/mentor/reviews` | mentor |
| GET | `/api/v1/mentor/student-resumes` | mentor |
| GET | `/api/v1/mentor/student-resumes/{id}/download` | mentor |

### Resumes
| Method | Path | Auth |
| --- | --- | --- |
| GET | `/api/v1/resumes/me` | student |
| POST | `/api/v1/resumes/me` | student |
| PUT | `/api/v1/resumes/me/{id}` | student |
| DELETE | `/api/v1/resumes/me/{id}` | student |
| POST | `/api/v1/resumes/me/upload-pdf` | student |
| GET | `/api/v1/resumes/me/current` | student |
| DELETE | `/api/v1/resumes/me/current` | student |
| GET | `/api/v1/resume-templates` | pub |

### Payments
| Method | Path | Auth |
| --- | --- | --- |
| POST | `/api/v1/payment/create-order` | auth |
| POST | `/api/v1/payment/verify` | auth |

### Notifications
| Method | Path | Auth |
| --- | --- | --- |
| GET | `/api/v1/notifications/me` | auth |
| POST | `/api/v1/notifications/{id}/read` | auth |
| POST | `/api/v1/notifications/read-all` | auth |
| POST | `/api/v1/notifications/clear-all` | auth |

### Support
| Method | Path | Auth |
| --- | --- | --- |
| GET | `/api/v1/support/tickets` | auth |
| POST | `/api/v1/support/tickets` | auth |
| POST | `/api/v1/support/tickets/{id}/messages` | auth |

### Admin (subset — see Swagger for the full list)
Path prefix: `/api/v1/admin/…` (Practice, PlacementCompanies, LearningPaths, Assessments, ResumeTemplates, InterviewQuestions, StudentResumes, Support, Settings, Offer, PublishNotification). All `admin`-role.

### Health
| Method | Path | Auth |
| --- | --- | --- |
| GET | `/health/live` | pub |
| GET | `/health/ready` | pub |

*(Health lives outside `/api` so it's stable across versions.)*

## 8. Versioning policy

- **Additive changes** (new endpoint, new optional field, new optional query parameter) → stay in the current major version. Client keeps working.
- **Breaking changes** (removing a field, changing a type, changing status codes, renaming a route) → introduce `/api/v2/…`. Old version keeps running for at least one deprecation cycle.
- The `X-Api-Version` response header always reflects the version that served the request.
