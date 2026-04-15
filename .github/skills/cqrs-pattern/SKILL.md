---
name: cqrs-pattern
description: "Use when implementing CQRS pattern with command and query separation and MediatR integration"
---

# CQRS Pattern Skill Guide

## Overview
Command Query Responsibility Segregation (CQRS) separates read and write operations, enabling independent optimization of each.

## Core Concepts

### Commands
- **Purpose**: Modify data (Create, Update, Delete)
- **Return**: Response object or void
- **Side Effects**: YES - commands change state
- **Caching**: NO - never cache command results

### Queries
- **Purpose**: Retrieve data (Read-only)
- **Return**: Data object (DTO)
- **Side Effects**: NO - queries don't modify state
- **Caching**: YES - safe to cache query results

### Handlers
- **Command Handlers**: Process commands and return responses
- **Query Handlers**: Process queries and return data
- **Responsibility**: Business logic implementation

## Implementation Pattern

### Command Structure
```csharp
// Command Definition (in Application layer)
public record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    int Stock
) : ICommand<CreateProductResponse>;

// Handler Implementation
public class CreateProductCommandHandler : 
    ICommandHandler<CreateProductCommand, CreateProductResponse>
{
    private readonly IProductRepository _repository;

    public async Task<CreateProductResponse> Handle(
        CreateProductCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Validate command
        // 2. Create domain entity
        // 3. Persist to database
        // 4. Return response
        var product = new Product(request.Name, request.Price, ...);
        await _repository.AddAsync(product);
        return new CreateProductResponse(product.Id, "Success");
    }
}
```

### Query Structure
```csharp
// Query Definition (in Application layer)
public record GetAllProductsQuery : IQuery<List<ProductDto>>;

// Handler Implementation
public class GetAllProductsQueryHandler : 
    IQueryHandler<GetAllProductsQuery, List<ProductDto>>
{
    private readonly IProductRepository _repository;

    public async Task<List<ProductDto>> Handle(
        GetAllProductsQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Query database
        // 2. Transform to DTOs
        // 3. Return data
        var products = await _repository.GetAllAsync();
        return products
            .Select(p => new ProductDto(p.Id, p.Name, ...))
            .ToList();
    }
}
```

## MediatR Integration

### Setup in Program.cs
```csharp
// Register MediatR with handler assembly scanning
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<GetAllProductsQuery>()
);
```

### Usage in Controllers
```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Query Example
    [HttpGet]
    public async Task<ActionResult<List<ProductDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var query = new GetAllProductsQuery();
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    // Command Example
    [HttpPost]
    public async Task<ActionResult<CreateProductResponse>> Create(
        CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
}
```

## Design Patterns

### Single Responsibility
- One handler per command/query
- One database operation per handler
- Clear, focused business logic

### Command Pattern
- Encapsulates requests as objects
- Enables undo/redo operations
- Queue commands for later execution

### Strategy Pattern
- Different handlers for different operations
- Easy to add new operations without modifying existing code
- Adheres to Open/Closed Principle

## Best Practices

1. **Naming Convention**: Use verb prefixes (Get, Create, Update, Delete)
2. **Response Objects**: Define specific response DTOs for each command
3. **Error Handling**: Throw exceptions for errors, handle in controller
4. **Validation**: Validate in handler before processing
5. **Async/Await**: Use async handlers for I/O operations
6. **Cancellation**: Support CancellationToken for long operations

## Anti-Patterns to Avoid

❌ Mixing commands and queries in same handler
❌ Business logic in controller
❌ Multiple responsibilities in one handler
❌ Ignoring CancellationToken
❌ Throwing HTTP exceptions from handlers

## Performance Considerations

- **Queries**: Can be cached, consider read models
- **Commands**: No caching, ensure idempotency where possible
- **Separation**: Scale read and write databases independently
- **Read Models**: Denormalize data for query performance
