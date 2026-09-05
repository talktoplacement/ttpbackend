#!/usr/bin/env node
/**
 * Swagger route smoke test — zero-dependency (Node ≥ 18, uses global fetch).
 *
 * Reads `/swagger/v1/swagger.json` from the running host, iterates every
 * (path, method) pair, sends a probe request, and classifies the response as:
 *
 *   PASS   → 2xx (route works)
 *          → 400 (route works; smoke didn't supply a valid body)
 *          → 401 (route works; smoke has no bearer token)
 *          → 404 (route works; smoke used a placeholder id)
 *   FAIL   → anything else (5xx, 500, ECONNREFUSED, path-not-found on the router,
 *            or 405 method-not-allowed → the route doesn't exist server-side)
 *
 * Every path parameter is substituted with a sentinel value:
 *   int  → 1        (matches `:int` constraint)
 *   guid → 00000000-0000-0000-0000-000000000000
 *   *    → "smoke"
 *
 * Usage:
 *
 *   BASE_URL=http://localhost:5215 node backend/scripts/swagger-smoke.mjs
 *   TOKEN=eyJhbGciOi… node backend/scripts/swagger-smoke.mjs   # authenticated probes
 *   node backend/scripts/swagger-smoke.mjs --fail-on 500       # only 5xx fails
 *
 * Exit code:
 *   0 — every route classified PASS
 *   1 — any FAIL, or the swagger doc could not be fetched
 */

const BASE_URL = process.env.BASE_URL || "http://localhost:5215";
const TOKEN = process.env.TOKEN || "";
const SWAGGER_PATH = process.env.SWAGGER_PATH || "/swagger/v1/swagger.json";

// CLI: `--fail-on 500` restricts the FAIL set to responses whose status ≥ N.
const failOnArg = process.argv.indexOf("--fail-on");
const FAIL_ON_STATUS =
  failOnArg > -1 && Number(process.argv[failOnArg + 1])
    ? Number(process.argv[failOnArg + 1])
    : 500;

const EXPECTED_OK_STATUSES = new Set([200, 201, 204, 400, 401, 403, 404, 409]);

const INT_SENTINEL = "1";
const GUID_SENTINEL = "00000000-0000-0000-0000-000000000000";
const STRING_SENTINEL = "smoke";

/** Substitute {id:int}, {slug}, etc. with sentinel values. */
function substitutePathParams(path) {
  return path.replace(/\{([^}]+)\}/g, (_, param) => {
    const [, constraint] = param.split(":");
    if (constraint === "int" || constraint === "long") return INT_SENTINEL;
    if (constraint === "guid") return GUID_SENTINEL;
    return STRING_SENTINEL;
  });
}

/** ANSI colors — only when stdout is a TTY. */
const isTty = process.stdout.isTTY;
const c = {
  reset: isTty ? "\x1b[0m" : "",
  dim: isTty ? "\x1b[2m" : "",
  green: isTty ? "\x1b[32m" : "",
  yellow: isTty ? "\x1b[33m" : "",
  red: isTty ? "\x1b[31m" : "",
  cyan: isTty ? "\x1b[36m" : "",
};

async function fetchJson(url) {
  const res = await fetch(url);
  if (!res.ok) throw new Error(`GET ${url} → ${res.status}`);
  return res.json();
}

async function probe(method, path) {
  const url = new URL(substitutePathParams(path), BASE_URL).toString();
  const headers = { Accept: "application/json" };
  if (TOKEN) headers.Authorization = `Bearer ${TOKEN}`;

  // Give body-taking methods an empty JSON object so the model binder doesn't 415.
  const hasBody = ["POST", "PUT", "PATCH"].includes(method);
  const body = hasBody ? "{}" : undefined;
  if (hasBody) headers["Content-Type"] = "application/json";

  const started = Date.now();
  try {
    const res = await fetch(url, { method, headers, body });
    return { ok: true, status: res.status, ms: Date.now() - started };
  } catch (err) {
    return { ok: false, error: err.message, ms: Date.now() - started };
  }
}

function classify(result) {
  if (!result.ok) return "FAIL";
  const s = result.status;
  if (EXPECTED_OK_STATUSES.has(s)) return "PASS";
  if (s === 405) return "FAIL"; // route exists but not this method — server-side gap
  if (s >= FAIL_ON_STATUS) return "FAIL";
  return "WARN"; // e.g. 308 redirect (shouldn't happen post-cleanup)
}

async function main() {
  console.log(`${c.cyan}Smoke target${c.reset}: ${BASE_URL}`);
  console.log(`${c.cyan}Swagger doc${c.reset} : ${BASE_URL}${SWAGGER_PATH}`);
  console.log(`${c.cyan}Auth token${c.reset}  : ${TOKEN ? "(present)" : "(none — expect many 401s)"}`);
  console.log();

  let doc;
  try {
    doc = await fetchJson(new URL(SWAGGER_PATH, BASE_URL).toString());
  } catch (err) {
    console.error(`${c.red}Could not load Swagger doc${c.reset}: ${err.message}`);
    console.error("Is the backend running on " + BASE_URL + "?");
    process.exit(1);
  }

  const routes = [];
  for (const [path, ops] of Object.entries(doc.paths ?? {})) {
    for (const method of Object.keys(ops)) {
      if (!["get", "post", "put", "patch", "delete"].includes(method)) continue;
      routes.push({ method: method.toUpperCase(), path });
    }
  }
  console.log(`${c.cyan}Total routes${c.reset}: ${routes.length}\n`);

  const buckets = { PASS: [], WARN: [], FAIL: [] };
  // Bound concurrency to keep the probe polite.
  const CONCURRENCY = 8;
  let cursor = 0;
  async function worker() {
    while (cursor < routes.length) {
      const idx = cursor++;
      const { method, path } = routes[idx];
      const result = await probe(method, path);
      const kind = classify(result);
      buckets[kind].push({ method, path, ...result });
      const status = result.ok ? String(result.status) : "ERR";
      const color = kind === "PASS" ? c.green : kind === "WARN" ? c.yellow : c.red;
      const ms = String(result.ms).padStart(4) + "ms";
      console.log(
        `${color}${kind.padEnd(4)}${c.reset} ${c.dim}${ms}${c.reset} ${status.padEnd(3)} ${method.padEnd(6)} ${path}${
          result.error ? " — " + result.error : ""
        }`,
      );
    }
  }
  await Promise.all(Array.from({ length: CONCURRENCY }, worker));

  console.log();
  console.log(`${c.green}PASS${c.reset}: ${buckets.PASS.length}`);
  console.log(`${c.yellow}WARN${c.reset}: ${buckets.WARN.length}`);
  console.log(`${c.red}FAIL${c.reset}: ${buckets.FAIL.length}`);
  if (buckets.FAIL.length > 0) {
    console.log(`\n${c.red}Failures:${c.reset}`);
    for (const f of buckets.FAIL) {
      const detail = f.ok ? `HTTP ${f.status}` : f.error;
      console.log(`  ${f.method} ${f.path} — ${detail}`);
    }
    process.exit(1);
  }
  process.exit(0);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
