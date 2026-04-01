# ─── Stage 1: Build ───────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and all project files first (layer-caches NuGet restore)
COPY NetAPI.sln .
COPY src/NetAPI.Domain/NetAPI.Domain.csproj             src/NetAPI.Domain/
COPY src/NetAPI.Application/NetAPI.Application.csproj   src/NetAPI.Application/
COPY src/NetAPI.Infrastructure/NetAPI.Infrastructure.csproj src/NetAPI.Infrastructure/
COPY src/NetAPI.Api/NetAPI.Api.csproj                   src/NetAPI.Api/

RUN dotnet restore src/NetAPI.Api/NetAPI.Api.csproj

# Copy everything and build
COPY . .
RUN dotnet build src/NetAPI.Api/NetAPI.Api.csproj -c Release --no-restore

# ─── Stage 2: Publish ─────────────────────────────────────────────────────────
FROM build AS publish
RUN dotnet publish src/NetAPI.Api/NetAPI.Api.csproj -c Release -o /app/publish --no-build

# ─── Stage 3: Runtime ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
USER appuser

EXPOSE 8080
EXPOSE 8081

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "NetAPI.Api.dll"]
