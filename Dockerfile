# syntax=docker/dockerfile:1.7

# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first (leverages Docker layer cache) — copy only project/props files.
COPY Directory.Build.props Directory.Packages.props ./
COPY CareerPlatform.sln ./
COPY src/CareerPlatform.Api/CareerPlatform.Api.csproj src/CareerPlatform.Api/
COPY tests/CareerPlatform.ArchitectureTests/CareerPlatform.ArchitectureTests.csproj tests/CareerPlatform.ArchitectureTests/
COPY tests/CareerPlatform.UnitTests/CareerPlatform.UnitTests.csproj tests/CareerPlatform.UnitTests/
COPY tests/CareerPlatform.IntegrationTests/CareerPlatform.IntegrationTests.csproj tests/CareerPlatform.IntegrationTests/
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

# Run as non-root. The base image ships an "app" user (uid 1654); use it.
USER app

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_NOLOGO=true \
    DOTNET_CLI_TELEMETRY_OPTOUT=true

EXPOSE 8080

COPY --from=build --chown=app:app /app/publish ./

# Liveness probe: the API exposes /health/live (no dependencies) and /health/ready (DB/Redis).
# Use curl if present, else the built-in dotnet-based fallback via wget/exit code.
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD wget --spider -q http://127.0.0.1:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "CareerPlatform.Api.dll"]
