---
name: dotnet-architect
description: 'Design scalable .NET backend solutions and apply architectural patterns. Use for: Clean Architecture, DDD, CQRS, Mediator pattern, layered architecture, domain modeling, repository pattern, dependency injection setup, .NET 8/9 project scaffolding, microservices, API design, integration optimization, bounded contexts, event-driven architecture.'
argument-hint: 'Describe the architectural challenge or .NET backend design task'
---

# .NET Architect Skill

## When to Use
- Designing a new .NET backend project structure (Clean Architecture, Onion, Hexagonal)
- Applying CQRS with MediatR, command/query separation
- Domain-Driven Design: aggregates, value objects, domain events
- Repository and Unit of Work patterns
- Microservices decomposition and inter-service communication
- Optimizing dependency injection and service registration in .NET
- Evaluating trade-offs between architectural approaches

## Procedure

### 1. Clarify Requirements
- Identify the domain complexity (simple CRUD vs rich domain logic)
- Determine team size and expected maintainability needs
- Choose target: monolith, modular monolith, or microservices

### 2. Select Architecture Pattern

| Scenario | Pattern |
|----------|---------|
| Complex domain logic | Clean Architecture + DDD |
| High read/write asymmetry | CQRS + MediatR |
| Multiple bounded contexts | Microservices or Modular Monolith |
| Simple CRUD API | Minimal API with service layer |

### 3. Generate Project Structure

**Clean Architecture (.NET 8/9)**
```
src/
  Domain/           # Entities, value objects, domain events, interfaces
  Application/      # Use cases, commands, queries, DTOs, validators
  Infrastructure/   # EF Core, repositories, external services
  API/              # Controllers or Minimal API endpoints, middleware
```

**CQRS with MediatR**
```csharp
// Command
public record CreateOrderCommand(Guid CustomerId, List<OrderItem> Items) : IRequest<Guid>;

// Handler
public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Guid>
{
    private readonly IOrderRepository _repository;
    public CreateOrderCommandHandler(IOrderRepository repository) => _repository = repository;

    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        var order = Order.Create(request.CustomerId, request.Items);
        await _repository.AddAsync(order, ct);
        return order.Id;
    }
}

// Registration in Program.cs
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateOrderCommand).Assembly));
```

### 4. Apply Best Practices
- Keep Domain layer free of framework dependencies
- Use `IRepository<T>` abstractions in Application, implementations in Infrastructure
- Register services with appropriate lifetimes (Scoped for EF DbContext, Singleton for caches)
- Use FluentValidation pipeline behaviors with MediatR for input validation
- Avoid anemic domain models — push business logic into entities/aggregates

### 5. Validate Design
- Check for circular dependencies between layers
- Ensure domain entities have no direct EF Core or HTTP references
- Verify all external I/O is behind interfaces (testability)
