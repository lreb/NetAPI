# NetAPI

Production-ready .NET 8 Web API template following **Clean Architecture**, **CQRS**, and **Domain-Driven Design** principles. Use this as a starting point for any new backend service.

---

## Table of Contents

1. [Architecture](#architecture)
2. [Project Structure](#project-structure)
3. [Features](#features)
4. [Prerequisites](#prerequisites)
5. [Running Locally](#running-locally)
6. [Configuration](#configuration)
7. [Secrets & Local Overrides](#secrets--local-overrides)
8. [Database Migrations](#database-migrations)
9. [Authentication](#authentication)
10. [API Versioning](#api-versioning)
11. [Health Checks](#health-checks)
12. [Testing](#testing)
13. [Docker](#docker)
14. [CI/CD](#cicd)
15. [Design Decisions](#design-decisions)

---

## Architecture

```
NetAPI.Api  →  NetAPI.Application  →  NetAPI.Domain
                       ↓
              NetAPI.Infrastructure  →  NetAPI.Domain
```

Dependencies point inward. Inner layers never reference outer layers.

| Layer | Responsibility |
|---|---|
| **Domain** | Entities, value objects, domain events, interfaces, exceptions. No framework dependencies. |
| **Application** | CQRS handlers (MediatR), DTOs, validators (FluentValidation), mapping (Mapster). |
| **Infrastructure** | EF Core, repositories, unit of work, Redis cache, database seeder. |
| **Api** | Controllers, middleware, Swagger, Program.cs bootstrap. |

---

## Project Structure

```
src/
├── NetAPI.Domain/
│   ├── Common/           BaseEntity, IDomainEvent, ValueObject
│   ├── Entities/         Product (example aggregate root)
│   ├── Events/           Domain events raised by entities
│   ├── Exceptions/       DomainException, NotFoundException
│   ├── Interfaces/       IRepository<T>, IProductRepository, IUnitOfWork
│   └── ValueObjects/     Money
│
├── NetAPI.Application/
│   ├── Behaviors/        LoggingBehavior, PerformanceBehavior, ValidationBehavior
│   ├── Commands/         CreateProductCommand/Handler, DeleteProductCommand/Handler
│   ├── DTOs/             ProductDto, PaginatedResult<T>
│   ├── Exceptions/       ValidationException
│   ├── Extensions/       ApplicationServiceExtensions (DI wiring)
│   ├── Queries/          GetProductById, GetAllProducts, GetPagedProducts
│   └── Validators/       CreateProductCommandValidator
│
├── NetAPI.Infrastructure/
│   ├── Extensions/       InfrastructureServiceExtensions (DI wiring)
│   └── Persistence/
│       ├── AppDbContext.cs
│       ├── AppDbContextSeed.cs
│       ├── UnitOfWork.cs
│       ├── Configurations/   ProductConfiguration (EF owned types, soft-delete filter)
│       └── Repositories/     Repository<T>, ProductRepository
│
└── NetAPI.Api/
    ├── Configuration/    ConfigureSwaggerOptions (per-version docs + JWT UI)
    ├── Controllers/V1/   ProductsController
    ├── Middleware/        GlobalExceptionMiddleware
    ├── Program.cs
    ├── appsettings.json
    └── appsettings.Development.json

tests/
├── NetAPI.UnitTests/
│   ├── Domain/           ProductTests (8), MoneyTests (5)
│   └── Application/      CreateProductCommandHandlerTests (7)
└── NetAPI.IntegrationTests/
    ├── ApiWebApplicationFactory.cs
    └── Controllers/      ProductsControllerTests
```

---

## Features

### Security
- **JWT Bearer authentication** — configurable issuer, audience, expiry, and signing key
- **Role-based & policy-based authorization** via `[Authorize(Roles = "Admin")]`
- **Input validation** — FluentValidation runs in the MediatR pipeline before every handler
- **CORS** — origins defined in `appsettings.json`, not hardcoded

### Data & Persistence
- **EF Core** with SQL Server and automatic retry-on-failure (5 retries, 30s max delay)
- **Repository + Unit of Work** pattern — handlers never touch `DbContext` directly
- **Soft delete** — `Repository<T>.Remove()` sets `IsDeleted = true`; a global query filter excludes soft-deleted rows automatically
- **EF owned types** — `Money` value object is persisted as columns on the `Products` table
- **Domain event dispatch** — `AppDbContext.SaveChangesAsync` dispatches events raised by entities
- **Database seeder** — seeds 3 products in Development; runs on startup

### Cross-Cutting
- **Global exception middleware** — maps `ValidationException` → 422, `NotFoundException` → 404, `DomainException` → 400, all others → 500; every response includes a `traceId`
- **Serilog** — bootstrap logger (captures startup crashes), rolling file sink, `WithMachineName` enricher, config driven
- **Rate limiting** — `AspNetCoreRateLimit` with rules from `appsettings.json` (100 req/min, 1000 req/hour)
- **In-memory + Redis distributed cache** — Redis used when connection string is present; falls back to in-memory
- **API versioning** — URL segment (`/api/v1/`) and `X-Api-Version` header

### Observability
- **Health checks**: `/health` (SQL + Redis), `/health/ready` (SQL only), `/health/live` (always up)
- **Swagger/OpenAPI** — per-version docs, JWT "Authorize" button in UI
- **Performance behavior** — logs a warning for any handler exceeding 500ms

### MediatR Pipeline Order
```
Request → LoggingBehavior → PerformanceBehavior → ValidationBehavior → Handler
```

---

## Prerequisites

| Tool | Version |
|---|---|
| .NET SDK | 8.0+ |
| Docker Desktop | 4.x+ (for compose) |
| PostgreSQL | 14+ (local install or remote server) |
| Redis | 7+ (optional — falls back to in-memory cache) |
| EF Core Tools | `dotnet tool install --global dotnet-ef` |

---

## Running Locally

### Option A — Docker Compose (recommended)

Starts the API and Redis with health-check dependencies (PostgreSQL must be reachable — configure connection string first):

```bash
docker compose up --build
```

### Option B — Native

```bash
# 1. Restore & build
dotnet restore
dotnet build

# 2. Set your local connection string (see Secrets & Local Overrides below)

# 3. Apply migrations
dotnet ef database update `
  --project src/NetAPI.Infrastructure `
  --startup-project src/NetAPI.Api

# 4. Run
dotnet run --project src/NetAPI.Api
```

Swagger UI: `https://localhost:5001/swagger`

---

## Configuration

All settings live in `src/NetAPI.Api/appsettings.json`. Override per-environment in `appsettings.{Environment}.json` or via environment variables.

| Section | Key | Description |
|---|---|---|
| `ConnectionStrings` | `DefaultConnection` | PostgreSQL connection string (Npgsql format) |
| `ConnectionStrings` | `Redis` | Redis connection string |
| `Jwt` | `Key` | Signing key — **must be ≥ 32 chars**. Never commit a real value. |
| `Jwt` | `Issuer` | Token issuer (default: `NetAPI`) |
| `Jwt` | `Audience` | Token audience (default: `NetAPI.Client`) |
| `Jwt` | `ExpiresInMinutes` | Token lifetime (default: `60`) |
| `Cors` | `AllowedOrigins` | JSON array of allowed frontend origins |
| `Serilog` | `MinimumLevel` | Log levels per namespace |
| `IpRateLimiting` | `GeneralRules` | Rate limit rules array |

> **Security**: `appsettings.json` and `appsettings.Development.json` are committed to git and must contain **only placeholders** — never real passwords, IPs, or keys. Supply real values via `appsettings.Development.local.json` (gitignored) or environment variables. See [Secrets & Local Overrides](#secrets--local-overrides).

---

## Secrets & Local Overrides

Real credentials are **never committed**. The safe pattern:

```
appsettings.json                  ← committed, placeholders only
appsettings.Development.json      ← committed, placeholders only
appsettings.Development.local.json ← gitignored, your real local values
```

### Step 1 — Create your local override file

Create `src/NetAPI.Api/appsettings.Development.local.json` (automatically loaded in Development, never committed):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=192.168.x.x;Database=NetAPI_Dev;Username=youruser;Password=yourpassword;",
    "Redis": "localhost:6379"
  },
  "Jwt": {
    "Key": "your-local-secret-key-minimum-32-characters!!"
  }
}
```

### Step 2 — Alternatively, use .NET User Secrets (recommended for teams)

```bash
# Initialize (one-time per developer)
dotnet user-secrets init --project src/NetAPI.Api

# Set individual secrets
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Host=192.168.x.x;Database=NetAPI_Dev;Username=youruser;Password=yourpassword;" \
  --project src/NetAPI.Api

dotnet user-secrets set "Jwt:Key" "your-local-secret-min-32-chars!!" \
  --project src/NetAPI.Api

# List all current secrets
dotnet user-secrets list --project src/NetAPI.Api

# Remove a secret
dotnet user-secrets remove "Jwt:Key" --project src/NetAPI.Api

# Clear all secrets
dotnet user-secrets clear --project src/NetAPI.Api
```

> User secrets are stored in `%APPDATA%\Microsoft\UserSecrets\<guid>\secrets.json` on Windows — outside the repo, never committed.

### Environment Variables (CI/CD & Production)

ASP.NET Core maps environment variables using `__` as the section separator:

```bash
# Linux / Docker
export ConnectionStrings__DefaultConnection="Host=prod-db;Database=NetAPI;Username=produser;Password=secret;"
export Jwt__Key="production-key-minimum-32-characters!!"

# docker-compose.yml / Kubernetes secret
environment:
  - ConnectionStrings__DefaultConnection=Host=prod-db;...
  - Jwt__Key=...
```

### What is gitignored

| Pattern | File type |
|---|---|
| `appsettings.*.local.json` | All local environment overrides |
| `secrets.json` | .NET User Secrets local store |
| `.env`, `.env.*` | Docker / shell environment files |
| `*.pfx`, `*.pem`, `*.key` | Certificates and private keys |

---

## Database Migrations

All `dotnet ef` commands require the EF Core CLI tools:

```bash
dotnet tool install --global dotnet-ef
dotnet tool update --global dotnet-ef   # keep up to date
```

> All commands below use PowerShell syntax (backtick `` ` `` for line continuation). On Linux/macOS replace `` ` `` with `\`.

---

### Create a new migration

Run after changing any `Entity`, `IEntityTypeConfiguration`, or owned-type mapping:

```powershell
dotnet ef migrations add <MigrationName> `
  --project src/NetAPI.Infrastructure `
  --startup-project src/NetAPI.Api
```

Example:
```powershell
dotnet ef migrations add AddProductCategory `
  --project src/NetAPI.Infrastructure `
  --startup-project src/NetAPI.Api
```

This creates two files under `src/NetAPI.Infrastructure/Migrations/`:
- `<timestamp>_<MigrationName>.cs` — the `Up()` and `Down()` methods
- `AppDbContextModelSnapshot.cs` — updated automatically (do not edit manually)

---

### Apply migrations to the database

```powershell
# Apply all pending migrations
dotnet ef database update `
  --project src/NetAPI.Infrastructure `
  --startup-project src/NetAPI.Api

# Apply up to a specific migration (partial rollforward)
dotnet ef database update <MigrationName> `
  --project src/NetAPI.Infrastructure `
  --startup-project src/NetAPI.Api
```

---

### Roll back — revert the last migration

```powershell
# Revert the database to the previous migration
dotnet ef database update <PreviousMigrationName> `
  --project src/NetAPI.Infrastructure `
  --startup-project src/NetAPI.Api

# Then remove the migration file (only if NOT yet pushed to shared repo)
dotnet ef migrations remove `
  --project src/NetAPI.Infrastructure `
  --startup-project src/NetAPI.Api
```

> `migrations remove` deletes the last migration file only. It fails if the migration has already been applied to the database — run `database update <previous>` first.

---

### Roll back to a specific point

```powershell
# Roll database back to the state after InitialCreate
dotnet ef database update InitialCreate `
  --project src/NetAPI.Infrastructure `
  --startup-project src/NetAPI.Api

# Roll back ALL migrations (empty database, schema dropped)
dotnet ef database update 0 `
  --project src/NetAPI.Infrastructure `
  --startup-project src/NetAPI.Api
```

---

### List migrations and their status

```powershell
dotnet ef migrations list `
  --project src/NetAPI.Infrastructure `
  --startup-project src/NetAPI.Api
```

Output marks each migration as `(Pending)` or `(Applied)`.

---

### Generate a SQL script instead of applying directly

Useful for reviewing changes before applying to production:

```powershell
# Full script (all migrations)
dotnet ef migrations script `
  --project src/NetAPI.Infrastructure `
  --startup-project src/NetAPI.Api `
  --output migrations.sql

# Idempotent script (safe to run multiple times — checks migration history table)
dotnet ef migrations script --idempotent `
  --project src/NetAPI.Infrastructure `
  --startup-project src/NetAPI.Api `
  --output migrations_idempotent.sql

# Delta script from one migration to another
dotnet ef migrations script InitialCreate AddProductCategory `
  --project src/NetAPI.Infrastructure `
  --startup-project src/NetAPI.Api
```

---

### Common errors

| Error | Cause | Fix |
|---|---|---|
| `No process is on the other end of the pipe` | Wrong connection type (Shared Memory vs TCP) | Change connection string to `Host=localhost;Port=5432;...` |
| `Host not found` | PostgreSQL unreachable | Check host/port, firewall, and that the server is running |
| `Already an object named '...' in the database` | Migration applied twice | Run `migrations list` to check applied state |
| `HostAbortedException` in output | EF CLI intentionally stops the host after scanning — **not a real error** | Migration created successfully; ignore the `[FTL]` line |
| `Your startup project doesn't reference Microsoft.EntityFrameworkCore.Design` | Missing design package | Already added to `NetAPI.Api.csproj` with `PrivateAssets=all` |

---

## Authentication

The API uses **JWT Bearer** tokens. To call a protected endpoint:

1. Obtain a token from your identity provider (or generate a test token via `ApiWebApplicationFactory.GenerateTestJwt()` in tests).
2. Add the header: `Authorization: Bearer <token>`
3. In Swagger UI, click **Authorize** and paste the token.

Controllers are `[Authorize]` by default. Public endpoints must be explicitly decorated with `[AllowAnonymous]`.

---

## API Versioning

Versioning is read from **both** the URL and the request header:

```
GET /api/v1/products
X-Api-Version: 1.0
```

To add v2: create `Controllers/V2/` and decorate with `[ApiVersion("2.0")]`.

---

## Health Checks

| Endpoint | Tags checked | Use for |
|---|---|---|
| `GET /health` | `db`, `cache` | Full readiness (all dependencies) |
| `GET /health/ready` | `db` | Kubernetes readiness probe |
| `GET /health/live` | _(none)_ | Kubernetes liveness probe |

---

## Testing

### Unit Tests

Pure, fast tests with no I/O. Framework: **xUnit + Moq**.

```bash
dotnet test tests/NetAPI.UnitTests/NetAPI.UnitTests.csproj
```

Coverage:
- `Domain/ProductTests.cs` — entity invariants, domain rule enforcement
- `Domain/MoneyTests.cs` — value object equality and validation
- `Application/CreateProductCommandHandlerTests.cs` — handler logic with mocked repository and UoW

### Integration Tests

Spins up the real ASP.NET Core pipeline with **EF Core in-memory** database. Tests issue HTTP requests and assert real responses.

```bash
dotnet test tests/NetAPI.IntegrationTests/NetAPI.IntegrationTests.csproj
```

```bash
# All tests together
dotnet test NetAPI.sln
```

---

## Docker

### Dockerfile

Multi-stage build (SDK → runtime). Final image runs as a non-root user (`app`).

```
Stage 1: mcr.microsoft.com/dotnet/sdk:8.0      — restore + publish
Stage 2: mcr.microsoft.com/dotnet/aspnet:8.0   — runtime only
```

### docker-compose.yml

| Service | Image | Port |
|---|---|---|
| `api` | local build | `5000:8080`, `5001:8081` |
| `redis` | `redis:7-alpine` | `6379:6379` |

PostgreSQL is **external** — point `ConnectionStrings__DefaultConnection` at your server via environment variable or `appsettings.Development.local.json`. The API container depends on `redis` being healthy before starting.

---

## CI/CD

`.github/workflows/ci-cd.yml` runs on every push to `main` and on pull requests:

| Step | What it does |
|---|---|
| Restore & Build | `dotnet build` |
| Vulnerability scan | `dotnet list package --vulnerable` — fails the pipeline if any high/critical packages found |
| Unit Tests | `dotnet test` with coverage (Coverlet) |
| Integration Tests | Full ASP.NET Core pipeline with in-memory DB |
| Docker Build & Push | Builds image, pushes to GitHub Container Registry (`ghcr.io`) on `main` |

---

## Design Decisions

| Decision | Rationale |
|---|---|
| **PostgreSQL via Npgsql** | Open-source, production-grade, lower licensing cost vs SQL Server; Npgsql is first-class EF Core provider |
| **Mapster over AutoMapper** | Faster (compile-time expressions), no reflection overhead, no known CVEs |
| **Soft delete** | Preserves audit history; no data loss from accidental deletes |
| **EF owned types for value objects** | No extra join table; `Money` columns live directly on `Products` |
| **Domain events dispatched in `SaveChangesAsync`** | Keeps events atomic with the persistence transaction; swap for outbox pattern in distributed scenarios |
| **Bootstrap Serilog logger** | Captures fatal startup exceptions before the DI container is ready |
| **Config-driven CORS + rate limits** | No code change needed to adjust allowed origins or rate rules per environment |
| **Three health endpoints** | Kubernetes requires separate liveness and readiness probes; `/health` covers full dependency checks |
| **`ClockSkew = 30s`** | Tight skew tolerance reduces the window for token replay attacks |
| **`appsettings.*.local.json` for secrets** | Gitignored overrides let each developer supply real credentials without risk of committing them |
