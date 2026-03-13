# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Restore — copy project files first to leverage layer cache
COPY src/BallastLane.Domain/BallastLane.Domain.csproj             src/BallastLane.Domain/
COPY src/BallastLane.Application/BallastLane.Application.csproj   src/BallastLane.Application/
COPY src/BallastLane.Infrastructure/BallastLane.Infrastructure.csproj src/BallastLane.Infrastructure/
COPY src/BallastLane.API/BallastLane.API.csproj                    src/BallastLane.API/

RUN dotnet restore src/BallastLane.API/BallastLane.API.csproj

# Build & publish
COPY src/ src/
RUN dotnet publish src/BallastLane.API/BallastLane.API.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# ── Stage 2: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

# Non-root user for security; pre-create DataProtection dir so the named volume
# is initialized with appuser ownership (Docker copies image dir perms on first mount).
RUN addgroup -S appgroup && adduser -S appuser -G appgroup \
    && mkdir -p /home/appuser/.aspnet/DataProtection-Keys \
    && chown -R appuser:appgroup /home/appuser/.aspnet
USER appuser

COPY --from=build --chown=appuser:appgroup /app/publish .

# ASP.NET Core listens on 8080 in non-Development environments by default
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "BallastLane.API.dll"]
