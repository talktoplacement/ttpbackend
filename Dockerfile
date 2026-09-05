# syntax=docker/dockerfile:1.7

# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first (leverages Docker layer cache) — copy only project/props files.
COPY Directory.Build.props Directory.Packages.props ./
COPY CareerPlatform.sln ./
COPY src/CareerPlatform.Api/CareerPlatform.Api.csproj src/CareerPlatform.Api/

# The test projects are deliberately NOT copied. Both the restore and the publish
# below target CareerPlatform.Api.csproj directly rather than the solution, so the
# test csproj files were never used — they only forced the deployment repo to carry
# a tests/ tree it does not need, and the build failed outright when it was absent.
# Tests run in CI and locally against the solution, not inside the runtime image.
RUN dotnet restore src/CareerPlatform.Api/CareerPlatform.Api.csproj

# Copy the rest and publish a self-contained-free framework-dependent build.
COPY src/ src/
RUN dotnet publish src/CareerPlatform.Api/CareerPlatform.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create the two mount points as root and hand them to the non-root runtime user,
# BEFORE switching to that user.
#
# This is what makes the named volumes usable. When Docker mounts an empty named
# volume over a path that already exists in the image, it seeds the volume with that
# path's contents AND its ownership. If the directory does not exist in the image,
# Docker creates the mount root:root instead and the `app` user gets
# "UnauthorizedAccessException: Access to the path '/app/storage/...' is denied".
#
#   /app/storage                        uploaded resumes (LocalFileStorage)
#   /home/app/.aspnet/DataProtection-Keys   keys that encrypt the auth cookie —
#       must persist, or every container recreate silently invalidates all sessions
RUN mkdir -p /app/storage /home/app/.aspnet/DataProtection-Keys \
    && chown -R app:app /app/storage /home/app/.aspnet

# Run as non-root. The base image ships an "app" user (uid 1654); use it.
USER app

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_NOLOGO=true \
    DOTNET_CLI_TELEMETRY_OPTOUT=true

EXPOSE 8080

COPY --from=build --chown=app:app /app/publish ./

# Operator-owned price list and code-execution language catalog, read at startup by
# AddOperatorPropertiesFile("application.properties") relative to the content root.
# Without it the app logs "No application.properties found" and the subscription
# catalog reconciler finds no plans, so the paid tiers never appear.
COPY --chown=app:app application.properties ./

# Liveness probe: the API exposes /health/live (no dependencies) and /health/ready (DB/Redis).
# Use curl if present, else the built-in dotnet-based fallback via wget/exit code.
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD wget --spider -q http://127.0.0.1:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "CareerPlatform.Api.dll"]
