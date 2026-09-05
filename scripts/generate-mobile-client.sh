#!/usr/bin/env bash
# Generate a mobile client (Kotlin / Swift / Dart / TypeScript) from the running
# backend's OpenAPI document at /swagger/v1/swagger.json.
#
# Usage
#   BASE_URL=http://localhost:5215 ./generate-mobile-client.sh kotlin ./out/kotlin
#   BASE_URL=https://api.example.dev ./generate-mobile-client.sh swift ./out/swift
#   ./generate-mobile-client.sh dart ./out/dart
#   ./generate-mobile-client.sh typescript-fetch ./out/ts
#
# Requires `openapi-generator-cli`. Install:
#   npm i -g @openapitools/openapi-generator-cli
# or use Homebrew:
#   brew install openapi-generator
#
# The generated client is fully typed against the /api/v1/* surface. Auth is Bearer JWT — the
# generator emits an ApiClient with `setBearerToken(...)` (Kotlin / Swift / Dart) or
# `configuration.accessToken` (TypeScript).

set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:5215}"
GENERATOR="${1:-}"
OUT_DIR="${2:-}"

if [ -z "$GENERATOR" ] || [ -z "$OUT_DIR" ]; then
  echo "Usage: $(basename "$0") <generator> <output-dir>"
  echo
  echo "Supported generators:"
  echo "  kotlin              — Android / KMP (OkHttp + Moshi)"
  echo "  swift5              — iOS / macOS (URLSession + Codable)"
  echo "  dart                — Flutter (dio-based)"
  echo "  typescript-fetch    — Web / React Native (native fetch)"
  echo
  echo "Environment:"
  echo "  BASE_URL   backend origin (default: http://localhost:5215)"
  exit 2
fi

if ! command -v openapi-generator-cli >/dev/null 2>&1; then
  echo "openapi-generator-cli not on PATH."
  echo "Install:   npm i -g @openapitools/openapi-generator-cli"
  echo "     or:   brew install openapi-generator"
  exit 3
fi

SPEC_URL="${BASE_URL%/}/swagger/v1/swagger.json"
echo "Spec:      $SPEC_URL"
echo "Generator: $GENERATOR"
echo "Output:    $OUT_DIR"
echo

# Fail fast if the spec is not reachable so we don't pollute the output dir.
if ! curl -fsS -o /dev/null "$SPEC_URL"; then
  echo "Could not reach $SPEC_URL — is the backend running?"
  exit 4
fi

mkdir -p "$OUT_DIR"

COMMON_OPTS=(
  --input-spec "$SPEC_URL"
  --generator-name "$GENERATOR"
  --output "$OUT_DIR"
  --skip-validate-spec
)

# Language-specific packaging.
case "$GENERATOR" in
  kotlin)
    openapi-generator-cli generate "${COMMON_OPTS[@]}" \
      --additional-properties=packageName=com.careerplatform.client,library=jvm-okhttp4,serializationLibrary=moshi
    ;;
  swift5)
    openapi-generator-cli generate "${COMMON_OPTS[@]}" \
      --additional-properties=projectName=CareerPlatformClient,responseAs=AsyncAwait
    ;;
  dart)
    openapi-generator-cli generate "${COMMON_OPTS[@]}" \
      --additional-properties=pubName=career_platform_client,nullSafe=true
    ;;
  typescript-fetch)
    openapi-generator-cli generate "${COMMON_OPTS[@]}" \
      --additional-properties=npmName=@career-platform/client,supportsES6=true,typescriptThreePlus=true
    ;;
  *)
    openapi-generator-cli generate "${COMMON_OPTS[@]}"
    ;;
esac

echo
echo "Done. Client written to $OUT_DIR"
