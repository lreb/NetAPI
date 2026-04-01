# NetAPI — Copilot Instructions

> This file is automatically loaded by GitHub Copilot as repository context.
> It tells AI agents exactly how this codebase is structured and how to contribute correctly.

---

## Architecture Overview

This project follows **Clean Architecture** with four layers. Dependencies always point inward:

```
NetAPI.Api  →  NetAPI.Application  →  NetAPI.Domain
                       ↓
              NetAPI.Infrastructure  →  NetAPI.Domain
```

| Project | Role | Must NOT depend on |
|---|---|---|
| `NetAPI.Domain` | Entities, value objects, domain events, interfaces, exceptions | Everything else |
| `NetAPI.Application` | CQRS commands/queries, DTOs, validators, MediatR pipeline behaviors | Infrastructure, Api |
| `NetAPI.Infrastructure` | EF Core, repositories, UoW, Redis, seeder | Api |
| `NetAPI.Api` | Controllers, middleware, Program.cs, Swagger | — |

---

## Where to Put New Code

### New domain entity
→ `src/NetAPI.Domain/Entities/`
- Inherit from `BaseEntity` (gives `Id`, `CreatedAt`, `UpdatedAt`, `IsDeleted`, domain events)
- All setters must be `private`; use factory method `static T Create(...)` instead of public constructors
- Raise domain events with `AddDomainEvent(new SomethingHappenedEvent(...))`
- Never reference EF Core, MediatR, or any infrastructure concern here

### New value object
→ `src/NetAPI.Domain/ValueObjects/`
- Inherit from `ValueObject`
- Override `GetEqualityComponents()` to define identity

### New command (write operation)
→ `src/NetAPI.Application/Commands/`
- `XxxCommand.cs` — `record` implementing `IRequest<T>`
- `XxxCommandHandler.cs` — `IRequestHandler<XxxCommand, T>`
- `src/NetAPI.Application/Validators/XxxCommandValidator.cs` — FluentValidation `AbstractValidator<XxxCommand>`
- The MediatR pipeline runs: `LoggingBehavior → PerformanceBehavior → ValidationBehavior` then your handler

### New query (read operation)
→ `src/NetAPI.Application/Queries/`
- `XxxQuery.cs` and `XxxQueryHandler.cs` in the same folder
- Return a DTO, never a domain entity

### New DTO
→ `src/NetAPI.Application/DTOs/`
- Mapping is configured in `ApplicationServiceExtensions.cs` using **Mapster** (`TypeAdapterConfig`)
- Flatten value objects in the Mapster config (see `ProductDto` → `Price`/`Currency` from `Money`)

### New repository interface
→ `src/NetAPI.Domain/Interfaces/`
- Extend `IRepository<T>` for generic operations
- Add domain-specific query methods to the entity-specific interface

### New repository implementation
→ `src/NetAPI.Infrastructure/Persistence/Repositories/`
- Extend `Repository<T>` and implement the domain-specific interface

### New EF Core entity configuration
→ `src/NetAPI.Infrastructure/Persistence/Configurations/`
- Implement `IEntityTypeConfiguration<T>`; it is auto-discovered by `ApplyConfigurationsFromAssembly`
- Map value objects as **owned types** (`OwnsOne`)
- Apply soft-delete global query filter: `.HasQueryFilter(e => !e.IsDeleted)`

### New API endpoint
→ `src/NetAPI.Api/Controllers/V1/`
- Inherit from `ControllerBase`
- Class attribute: `[ApiController]`, `[Route("api/v{version:apiVersion}/[controller]")]`, `[ApiVersion("1.0")]`
- Inject `IMediator` and dispatch commands/queries — never inject repositories or DbContext directly
- All endpoints require `[Authorize]` unless explicitly public

### New infrastructure service registration
→ `src/NetAPI.Infrastructure/Extensions/InfrastructureServiceExtensions.cs`

### New application service registration
→ `src/NetAPI.Application/Extensions/ApplicationServiceExtensions.cs`

---

## Key Patterns

### CQRS via MediatR
Commands mutate state. Queries read state. Neither shares a handler.

```csharp
// Command
public record CreateProductCommand(string Name, decimal Price, string Currency, int Stock)
    : IRequest<ProductDto>;

// Handler injects IProductRepository and IUnitOfWork — never DbContext
public class CreateProductCommandHandler(IProductRepository repo, IUnitOfWork uow, IMapper mapper)
    : IRequestHandler<CreateProductCommand, ProductDto> { ... }
```

### MediatR Pipeline (order matters)
1. `LoggingBehavior<,>` — logs request name, timing, and result
2. `PerformanceBehavior<,>` — warns when handler exceeds 500ms
3. `ValidationBehavior<,>` — runs all `IValidator<TRequest>` via FluentValidation; throws `ValidationException` on failure

### Domain Events
- Raised inside domain methods: `product.AddDomainEvent(new ProductCreatedEvent(...))`
- Dispatched by `AppDbContext.SaveChangesAsync` before clearing (see `AppDbContext.cs`)
- Currently dispatched in-process; replace with outbox pattern for distributed scenarios

### Soft Delete
- `BaseEntity.IsDeleted` is the flag
- `Repository<T>.Remove()` sets `IsDeleted = true` — no physical DELETE is ever issued
- EF Core global query filter `!e.IsDeleted` is applied in `XxxConfiguration.cs` for each entity

### Error Handling
`GlobalExceptionMiddleware` maps exceptions to HTTP responses:

| Exception | HTTP Status |
|---|---|
| `ValidationException` | 422 Unprocessable Entity + field errors dictionary |
| `NotFoundException` | 404 Not Found |
| `DomainException` | 400 Bad Request |
| Any other | 500 Internal Server Error |

Every error response includes `traceId` (from `HttpContext.TraceIdentifier`) for distributed log correlation.

### Unit of Work
- Always call `await _unitOfWork.SaveChangesAsync()` after mutations — never `_dbContext.SaveChangesAsync()` directly in handlers
- `IUnitOfWork` is registered as `Scoped`

---

## Configuration Sections (appsettings.json)

| Section | Purpose |
|---|---|
| `ConnectionStrings:DefaultConnection` | SQL Server connection string |
| `ConnectionStrings:Redis` | Redis connection string |
| `Jwt:Key` | **Must be ≥ 32 chars** — replace before deploying; never commit real keys |
| `Jwt:Issuer` / `Jwt:Audience` | Token issuer and audience |
| `Jwt:ExpiresInMinutes` | Token lifetime (default: 60) |
| `Cors:AllowedOrigins` | Array of allowed frontend origins |
| `Serilog` | Log levels and sink configuration |
| `IpRateLimiting` | Rate limit rules (100 req/min, 1000 req/hour by default) |

Environment-specific overrides in `appsettings.{Environment}.json`. Secrets must be provided via environment variables or a secrets manager — never hardcoded.

---

## API Versioning

Version is read from **both** the URL segment and the `X-Api-Version` request header:
- URL: `GET /api/v1/products`
- Header: `X-Api-Version: 1.0`

Add new versions by creating `Controllers/V2/` and decorating with `[ApiVersion("2.0")]`.

---

## Authentication & Authorization

- **JWT Bearer** is the default scheme configured in `Program.cs`
- Decorate controllers/actions: `[Authorize]`, `[Authorize(Roles = "Admin")]`, `[Authorize(Policy = "RequireXxx")]`
- The Swagger UI includes a "Authorize" button for Bearer tokens (configured in `ConfigureSwaggerOptions.cs`)

---

## Health Checks

| Endpoint | What it checks |
|---|---|
| `GET /health` | Full: SQL Server + Redis |
| `GET /health/ready` | SQL Server only (readiness probe) |
| `GET /health/live` | Always healthy (liveness probe) |

---

## Testing Strategy

### Unit Tests (`tests/NetAPI.UnitTests/`)
- `Domain/` — pure domain logic (no mocks needed): entity invariants, value object equality
- `Application/` — handler tests using `Moq` for repository/UoW interfaces
- Framework: **xUnit** + **Moq**

### Integration Tests (`tests/NetAPI.IntegrationTests/`)
- `ApiWebApplicationFactory` spins up the full API with **EF Core in-memory** database and a test JWT secret
- Tests issue real HTTP requests and assert status codes and payloads
- Use `GenerateTestJwt()` from the factory for endpoints requiring `[Authorize]`

---

## Conventions

- **No logic in controllers** — dispatch to MediatR, return the result
- **No DbContext injection outside Infrastructure** — use `IRepository<T>` or `IUnitOfWork`
- **No `public` setters** on domain entities — use domain methods
- **No `new Guid()`** in application/infrastructure — `Guid.NewGuid()` is called only in `BaseEntity`
- **No AutoMapper** — the project uses **Mapster** (faster, no reflection overhead)
- **Record types** for commands and queries (immutable by default)
- **PascalCase** for C# code; **camelCase** enforced in JSON responses via `JsonNamingPolicy.CamelCase`

---

## Running Locally (Quick Reference)

```bash
# With Docker Compose (recommended — includes SQL Server + Redis)
docker compose up --build

# Without Docker
dotnet restore
dotnet build
dotnet ef database update --project src/NetAPI.Infrastructure --startup-project src/NetAPI.Api
dotnet run --project src/NetAPI.Api
```

Swagger UI: `https://localhost:5001/swagger`

---

## Running Tests

```bash
dotnet test tests/NetAPI.UnitTests/NetAPI.UnitTests.csproj
dotnet test tests/NetAPI.IntegrationTests/NetAPI.IntegrationTests.csproj
```
