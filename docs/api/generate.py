#!/usr/bin/env python3
"""
Auto-generate `README.md` and `postman-collection.json` from every
`Features/**/*Endpoint.cs` file in the backend. The endpoint files are the single
source of truth; re-run this script after adding or renaming a route.

Usage: run from the repo root
    python3 backend/docs/api/generate.py

Output:
    backend/docs/api/README.md
    backend/docs/api/postman-collection.json
"""
from __future__ import annotations

import json
import pathlib
import re
from collections import defaultdict

ROOT = pathlib.Path(__file__).resolve().parents[3]  # repo root
FEATURES_DIR = ROOT / "backend" / "src" / "CareerPlatform.Api" / "Features"
OUT_DIR = ROOT / "backend" / "docs" / "api"

MAP_RE = re.compile(r'app\.Map(Get|Post|Put|Patch|Delete)\s*\(\s*"([^"]+)"', re.MULTILINE)
AUTHZ_RE = re.compile(r'\.RequireAuthorization\(\s*(?:"([^"]+)")?\s*\)')
ANON_RE = re.compile(r'\.AllowAnonymous\(\)')
NAME_RE = re.compile(r'\.WithName\("([^"]+)"\)')

BASE_URL_DEV = "http://localhost:5215"


def collect_endpoints() -> list[dict]:
    """Walk Features/, extract one record per Map<Verb>() call."""
    records: list[dict] = []
    for path in sorted(FEATURES_DIR.rglob("*Endpoint.cs")):
        text = path.read_text()

        doc_match = re.search(r'///\s*<summary>\s*(.*?)\s*</summary>', text, re.DOTALL)
        doc = ""
        if doc_match:
            doc = re.sub(r'\s*///\s*', ' ', doc_match.group(1)).strip()
            doc = re.sub(r'<[^>]+>', '', doc)
            doc = re.sub(r'\s+', ' ', doc).strip()

        matches = list(MAP_RE.finditer(text))
        for i, m in enumerate(matches):
            verb = m.group(1).upper()
            route = m.group(2)
            next_start = matches[i + 1].start() if i + 1 < len(matches) else len(text)
            window = text[m.end():next_start]

            auth = "Auth"
            if ANON_RE.search(window):
                auth = "Public"
            else:
                authz_match = AUTHZ_RE.search(window)
                if authz_match:
                    role = authz_match.group(1)
                    auth = f"Auth ({role})" if role else "Auth"

            name_match = NAME_RE.search(window)
            name = name_match.group(1) if name_match else ""

            feature = path.relative_to(FEATURES_DIR).parts[0]
            records.append({
                "feature": feature,
                "verb": verb,
                "route": route,
                "auth": auth,
                "name": name,
                "doc": doc,
                "file": str(path.relative_to(ROOT / "backend")),
            })

    records.sort(key=lambda r: (r["feature"].lower(), r["route"], r["verb"]))
    return records


def to_postman_url(route: str) -> dict:
    """Convert an ASP.NET route pattern into a Postman URL block with {{variables}}."""
    r = re.sub(r'\{([a-zA-Z]+)(:[a-zA-Z]+)?\}', lambda m: '{{' + m.group(1) + '}}', route)
    parts = [p for p in r.lstrip('/').split('/') if p]
    return {
        "raw": "{{baseUrl}}" + route,
        "host": ["{{baseUrl}}"],
        "path": parts,
    }


def emit_readme(records: list[dict]) -> str:
    by_feat: dict[str, list[dict]] = defaultdict(list)
    for r in records:
        by_feat[r["feature"]].append(r)

    lines: list[str] = []
    lines.append("# CareerPlatform API Reference")
    lines.append("")
    lines.append("Auto-generated from `backend/src/CareerPlatform.Api/Features/**/*Endpoint.cs`.  ")
    lines.append("Regenerate with `python3 backend/docs/api/generate.py`.")
    lines.append("")
    lines.append("## Overview")
    lines.append("")
    lines.append(f"- **Base URL (dev)**: `{BASE_URL_DEV}`")
    lines.append("- **Auth**: HTTP `Authorization: Bearer <jwt>` — issued by `POST /api/Auth/login` or Supabase.")
    lines.append("- **Content type**: `application/json` for all non-file endpoints.")
    lines.append("- **File uploads**: `multipart/form-data` with a single field named `file`.")
    lines.append("- **Errors**: RFC 7807 `application/problem+json` on 4xx/5xx.")
    lines.append("- **Rate limits**: sensitive endpoints (all state-changing + admin/mentor) are partitioned by user id or client IP.")
    lines.append("- **CSRF**: not enforced server-side; the JWT is the sole auth credential — mobile clients can ignore any CSRF header.")
    lines.append("- **CORS**: only relevant for browsers. Mobile apps hit the API directly.")
    lines.append("")
    lines.append(f"**Total endpoints:** {len(records)}")
    lines.append("")
    lines.append("## Auth flow (mobile)")
    lines.append("")
    lines.append("```")
    lines.append("POST /api/Auth/login")
    lines.append("Content-Type: application/json")
    lines.append("")
    lines.append("{ \"email\": \"...\", \"password\": \"...\" }")
    lines.append("")
    lines.append("→ 200 { \"accessToken\": \"eyJ…\", \"tokenType\": \"Bearer\", ... }")
    lines.append("```")
    lines.append("")
    lines.append("Store the `accessToken` securely (Keychain / EncryptedSharedPreferences) and attach it to every subsequent request:")
    lines.append("")
    lines.append("```")
    lines.append("Authorization: Bearer <accessToken>")
    lines.append("```")
    lines.append("")
    lines.append("A 401 response means the token expired or is invalid — refresh via `POST /api/Auth/login` again or (when refresh-token issuance lands) via the refresh endpoint.")
    lines.append("")
    lines.append("## Error shape")
    lines.append("")
    lines.append("Every 4xx/5xx returns a ProblemDetails body:")
    lines.append("")
    lines.append("```json")
    lines.append("{")
    lines.append("  \"type\": \"https://tools.ietf.org/html/rfc7231#section-6.5.4\",")
    lines.append("  \"title\": \"Not Found\",")
    lines.append("  \"status\": 404,")
    lines.append("  \"detail\": \"Session 42 was not found.\",")
    lines.append("  \"code\": \"Interview.SessionNotFound\"")
    lines.append("}")
    lines.append("```")
    lines.append("")
    lines.append("Validation errors (400) additionally include an `errors` object keyed on field name.")
    lines.append("")
    lines.append("## Feature index")
    lines.append("")
    for feat in sorted(by_feat.keys()):
        anchor = feat.lower()
        lines.append(f"- [{feat}](#{anchor}) — {len(by_feat[feat])} endpoint(s)")
    lines.append("")

    for feat in sorted(by_feat.keys()):
        lines.append(f"## {feat}")
        lines.append("")
        lines.append("| Method | Route | Auth | Handler name |")
        lines.append("|---|---|---|---|")
        for r in by_feat[feat]:
            lines.append(f"| {r['verb']} | `{r['route']}` | {r['auth']} | `{r['name']}` |")
        lines.append("")

    return "\n".join(lines)


def emit_postman(records: list[dict]) -> dict:
    by_feat: dict[str, list[dict]] = defaultdict(list)
    for r in records:
        by_feat[r["feature"]].append(r)

    collection: dict = {
        "info": {
            "name": "CareerPlatform API",
            "description": "Auto-generated. Set `baseUrl` and `token` under the collection variables tab.",
            "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json",
        },
        "variable": [
            {"key": "baseUrl", "value": BASE_URL_DEV},
            {"key": "token", "value": ""},
        ],
        "auth": {
            "type": "bearer",
            "bearer": [{"key": "token", "value": "{{token}}", "type": "string"}],
        },
        "item": [],
    }

    for feat in sorted(by_feat.keys()):
        folder: dict = {"name": feat, "item": []}
        for r in by_feat[feat]:
            req: dict = {
                "name": f"{r['verb']} {r['route']}",
                "request": {
                    "method": r["verb"],
                    "header": [{"key": "Accept", "value": "application/json"}],
                    "url": to_postman_url(r["route"]),
                    "description": r["doc"] or None,
                },
            }
            if r["verb"] in ("POST", "PUT", "PATCH"):
                req["request"]["header"].append(
                    {"key": "Content-Type", "value": "application/json"}
                )
                req["request"]["body"] = {
                    "mode": "raw",
                    "raw": "{}",
                    "options": {"raw": {"language": "json"}},
                }
            if r["auth"] == "Public":
                req["request"]["auth"] = {"type": "noauth"}
            folder["item"].append(req)
        collection["item"].append(folder)

    return collection


def main() -> None:
    records = collect_endpoints()
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    (OUT_DIR / "README.md").write_text(emit_readme(records))
    (OUT_DIR / "postman-collection.json").write_text(json.dumps(emit_postman(records), indent=2))
    features = {r["feature"] for r in records}
    print(f"wrote {OUT_DIR / 'README.md'}  ({len(records)} endpoints, {len(features)} features)")
    print(f"wrote {OUT_DIR / 'postman-collection.json'}")


if __name__ == "__main__":
    main()
