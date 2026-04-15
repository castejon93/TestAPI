---
name: clean-architecture
description: "Use when designing and implementing layered architecture with Clean Architecture principles and patterns"
---

# Clean Architecture Skill Guide

## Overview
Clean Architecture is a layered architectural pattern that emphasizes separation of concerns and independence of frameworks.

## Key Principles

### 1. Layer Independence
- **Domain Layer**: Core business logic, independent of all other layers
- **Application Layer**: Use cases and business workflows
- **Infrastructure Layer**: Data access and external services
- **Presentation Layer**: API controllers and HTTP concerns

### 2. Dependency Rule
- Dependencies should always point inward
- The Domain layer has no dependencies
- Application layer depends on Domain
- Infrastructure layer depends on Application and Domain
- Presentation layer depends on Application

### 3. Interface Segregation
- Define interfaces in the application/domain layer
- Implement them in the infrastructure layer
- Controllers depend on abstractions, not concrete classes

## Implementation Guidelines

### Domain Layer (Domain.csproj)
```
✓ Pure C# classes with business logic
✓ No framework dependencies
✓ Entities, value objects, domain events
✓ Business validation and rules
❌ No DbContext, no external services
❌ No ASP.NET Core dependencies
```

### Application Layer (Application.csproj)
```
✓ CQRS Handlers (Commands and Queries)
✓ DTOs (Data Transfer Objects)
✓ Validators
✓ Application services
✓ Mappers and transformations
❌ No direct database access
❌ No HTTP concerns
```

### Infrastructure Layer (Infrastructure.csproj)
```
✓ Entity Framework Core DbContext
✓ Repository implementations
✓ External service integrations
✓ Data access patterns
✓ Configuration and setup
❌ No business logic
❌ No HTTP concerns
```

### Presentation Layer (WebAPI.csproj)
```
✓ ASP.NET Core controllers
✓ HTTP routing and attributes
✓ Request/response mapping
✓ Error handling and status codes
❌ No business logic
❌ No direct database access
```

## Best Practices

1. **Keep Layers Thin**: Move business logic to lower layers
2. **Use Interfaces**: Define contracts between layers
3. **Dependency Injection**: Use DI container for loose coupling
4. **Avoid God Classes**: Follow Single Responsibility Principle
5. **Consistent Naming**: Use clear, descriptive names

## Anti-Patterns to Avoid

❌ Circular dependencies between layers
❌ Business logic in controllers
❌ Direct database queries in application layer
❌ Tightly coupled classes
❌ Large monolithic classes

## Testing Strategy

- **Unit Tests**: Test domain logic and validators
- **Integration Tests**: Test database interactions
- **API Tests**: Test controller endpoints
- Mock dependencies in lower layers during upper layer tests
