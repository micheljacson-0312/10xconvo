# ═══════════════════════════════════════════════════════════════════════════
#  TenXConvo API — Production Dockerfile
#
#  Build:  docker build -t tenxconvo-api .
#  Run:    docker run -p 5000:5000 --env-file .env.docker tenxconvo-api
#
#  Multi-stage build: SDK (large) for building → Runtime (small) for running
#  Final image ~120MB instead of ~900MB
# ═══════════════════════════════════════════════════════════════════════════

# ── Stage 1: Build ────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files first (Docker caches this layer if dependencies don't change)
COPY src/TenXConvo.Domain/TenXConvo.Domain.csproj              src/TenXConvo.Domain/
COPY src/TenXConvo.Application/TenXConvo.Application.csproj    src/TenXConvo.Application/
COPY src/TenXConvo.Infrastructure/TenXConvo.Infrastructure.csproj src/TenXConvo.Infrastructure/
COPY src/TenXConvo.API/TenXConvo.API.csproj                    src/TenXConvo.API/
RUN dotnet restore src/TenXConvo.API/TenXConvo.API.csproj

# Copy everything else and build
COPY . .
RUN dotnet publish src/TenXConvo.API/TenXConvo.API.csproj \
    -c Release -o /app/publish --no-restore

# ── Stage 2: Runtime ─────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Create upload directories
RUN mkdir -p wwwroot/uploads/avatars wwwroot/uploads/documents wwwroot/uploads/logos

# Copy published app
COPY --from=build /app/publish .

# ── Environment ──────────────────────────────────────────────────────────
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5000

EXPOSE 5000

# Health check — hits the /health endpoint every 30 seconds
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

ENTRYPOINT ["dotnet", "TenXConvo.API.dll"]
