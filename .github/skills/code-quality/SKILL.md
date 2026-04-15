---
name: code-quality
description: "Use when writing clean, maintainable code with best practices, naming conventions, and code organization"
---

# Code Quality & Best Practices Skill Guide

## Overview
Maintaining high code quality ensures the codebase remains maintainable, scalable, and bug-free.

## Clean Code Principles

### 1. Naming Conventions

#### Classes and Interfaces
```csharp
// ✓ Good: Clear, descriptive names
public class ProductRepository { }
public interface IProductRepository { }
public class CreateProductCommandHandler { }
public record CreateProductCommand { }

// ❌ Bad: Vague, unclear names
public class PR { }  // What is PR?
public class Handler1 { }
public class x { }
public class DoStuff { }
```

#### Methods
```csharp
// ✓ Good: Verb + Noun pattern
public async Task<Product> GetProductByIdAsync(int id) { }
public async Task CreateProductAsync(CreateProductCommand command) { }
public bool IsProductAvailable(int productId) { }
public void ValidateProduct(Product product) { }

// ❌ Bad: Unclear action
public async Task ProcessAsync(int x) { }
public void Handle() { }
public void Do(Product p) { }
```

#### Variables
```csharp
// ✓ Good: Clear, self-documenting
var productName = "Laptop";
var maximumPrice = 1000m;
var isProductAvailable = true;
var createdAt = DateTime.UtcNow;

// ❌ Bad: Cryptic abbreviations
var pn = "Laptop";
var mp = 1000m;
var avl = true;
var ca = DateTime.UtcNow;
```

### 2. Method Design

#### Single Responsibility
```csharp
// ✓ Good: One thing per method
public async Task<ProductDto> GetProductAsync(int id)
{
    /// Only responsible for retrieving and mapping
    var product = await _repository.GetByIdAsync(id);
    return MapToDto(product);
}

// ❌ Bad: Multiple responsibilities
public async Task<ProductDto> GetProductAsync(int id)
{
    var product = await _repository.GetByIdAsync(id);
    
    // Validation
    if (product == null)
        throw new Exception("Not found");
    
    // Logging
    _logger.LogInformation($"Retrieved {product.Name}");
    
    // Transformation
    var dto = new ProductDto { ... };
    
    // Notification
    await _emailService.SendAsync($"Product accessed: {product.Name}");
    
    return dto;
}
```

#### Method Length
```csharp
// ✓ Good: Keep methods short and focused
public async Task<CreateProductResponse> Handle(
    CreateProductCommand request,
    CancellationToken cancellationToken)
{
    /// Creates a new product
    var product = new Product(request.Name, request.Price, request.Stock);
    
    /// Validates product
    ValidateProduct(product);
    
    /// Persists product
    await _repository.AddAsync(product);
    await _repository.SaveChangesAsync();
    
    /// Returns response
    return new CreateProductResponse(product.Id, "Product created successfully");
}

private void ValidateProduct(Product product)
{
    if (string.IsNullOrEmpty(product.Name))
        throw new ArgumentException("Name is required");
    if (product.Price <= 0)
        throw new ArgumentException("Price must be positive");
}

// ❌ Bad: Long, complex method doing too much
public async Task<CreateProductResponse> Handle(...)
{
    // 50 lines of mixed concerns
    // Validation, transformation, error handling, logging all together
}
```

### 3. Comments and Documentation

#### XML Documentation
```csharp
// ✓ Good: XML comments for public APIs
/// <summary>
/// Retrieves a product by its identifier
/// </summary>
/// <param name="id">The product identifier to retrieve</param>
/// <param name="cancellationToken">Cancellation token for async operation</param>
/// <returns>The product if found, null otherwise</returns>
/// <exception cref="ArgumentException">Thrown when id is invalid</exception>
public async Task<ProductDto> GetProductByIdAsync(
    int id,
    CancellationToken cancellationToken)
{
    if (id <= 0)
        throw new ArgumentException("ID must be positive", nameof(id));

    var product = await _repository.GetByIdAsync(id);
    return product == null ? null : MapToDto(product);
}
```

#### Inline Comments
```csharp
// ✓ Good: Comments explain WHY, not WHAT
public async Task<List<ProductDto>> GetAllProductsAsync()
{
    // Use AsNoTracking() for read-only queries to improve performance
    // since we don't need to track changes for retrieval
    var products = await _repository.GetAllAsync();

    // Project to DTOs to prevent exposing domain entities
    return products
        .Select(p => MapToDto(p))
        .ToList();
}

// ❌ Bad: Comments state the obvious
public async Task<List<ProductDto>> GetAllProductsAsync()
{
    // Get all products from repository
    var products = await _repository.GetAllAsync();

    // Create list of DTOs
    var dtos = new List<ProductDto>();

    // Loop through products
    foreach (var product in products)
    {
        // Map to DTO
        dtos.Add(MapToDto(product));
    }

    // Return the list
    return dtos;
}
```

### 4. Error Handling

```csharp
// ✓ Good: Specific exception handling with meaningful messages
public async Task<ProductDto> GetProductByIdAsync(int id)
{
    if (id <= 0)
        throw new ArgumentException("Product ID must be greater than 0", nameof(id));

    var product = await _repository.GetByIdAsync(id);
    
    if (product == null)
        throw new InvalidOperationException($"Product with ID {id} not found");

    return MapToDto(product);
}

// In controller
[HttpGet("{id}")]
public async Task<ActionResult<ProductDto>> GetById(int id, CancellationToken cancellationToken)
{
    try
    {
        var product = await _handler.GetProductByIdAsync(id);
        return Ok(product);
    }
    catch (ArgumentException ex)
    {
        return BadRequest(ex.Message);
    }
    catch (InvalidOperationException ex)
    {
        return NotFound(ex.Message);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error retrieving product");
        return StatusCode(500, "An unexpected error occurred");
    }
}

// ❌ Bad: Swallowing exceptions or being too generic
public async Task<ProductDto> GetProductByIdAsync(int id)
{
    try
    {
        return MapToDto(await _repository.GetByIdAsync(id));
    }
    catch (Exception)
    {
        // Silently fail - very bad!
        return null;
    }
}
```

### 5. LINQ Best Practices

```csharp
// ✓ Good: Efficient LINQ queries
public async Task<List<ProductDto>> GetAvailableProductsAsync()
{
    /// Project directly to DTOs in LINQ before materializing
    return await _context.Products
        .Where(p => p.Stock > 0)  // Filter in database
        .OrderBy(p => p.Name)
        .AsNoTracking()  // Don't track for read-only
        .Select(p => new ProductDto(p.Id, p.Name, p.Price, ...))  // Project early
        .ToListAsync();
}

// ❌ Bad: Inefficient LINQ queries
public async Task<List<ProductDto>> GetAvailableProductsAsync()
{
    /// Loading all data into memory then filtering
    var allProducts = await _context.Products.ToListAsync();
    
    /// Filtering in memory is slower
    var available = allProducts.Where(p => p.Stock > 0).ToList();
    
    /// Materializing then projecting
    return available
        .Select(p => new ProductDto(p.Id, p.Name, p.Price, ...))
        .ToList();
}
```

### 6. DRY (Don't Repeat Yourself)

```csharp
// ✓ Good: Extract repeated logic into methods
private ProductDto MapToDto(Product product)
{
    /// Centralized mapping logic
    return new ProductDto(
        product.Id,
        product.Name,
        product.Description,
        product.Price,
        product.Stock,
        product.CreatedAt,
        product.UpdatedAt
    );
}

// Used consistently everywhere
public async Task<ProductDto> GetByIdAsync(int id)
{
    var product = await _repository.GetByIdAsync(id);
    return MapToDto(product);  // Reuse mapping
}

public async Task<List<ProductDto>> GetAllAsync()
{
    var products = await _repository.GetAllAsync();
    return products
        .Select(p => MapToDto(p))  // Reuse mapping
        .ToList();
}

// ❌ Bad: Repeated code in multiple places
public async Task<ProductDto> GetByIdAsync(int id)
{
    var product = await _repository.GetByIdAsync(id);
    return new ProductDto(product.Id, product.Name, ...);  // Mapping repeated
}

public async Task<List<ProductDto>> GetAllAsync()
{
    var products = await _repository.GetAllAsync();
    return products
        .Select(p => new ProductDto(p.Id, p.Name, ...))  // Same mapping repeated
        .ToList();
}
```

## Code Organization

### File Structure
```
Solution/
├── Domain/
│   ├── Entities/
│   │   └── Product.cs          // Domain entity
│   ├── Interfaces/
│   │   └── IProductRepository.cs // Repository interface
│   └── Events/
│       └── ProductCreatedEvent.cs

├── Application/
│   ├── DTOs/
│   │   └── ProductDto.cs       // Data transfer object
│   ├── CQRS/
│   │   ├── ICommand.cs
│   │   ├── IQuery.cs
│   │   └── IQueryHandler.cs
│   ├── Features/
│   │   └── Products/
│   │       ├── Commands/
│   │       │   ├── CreateProductCommand.cs
│   │       │   └── CreateProductCommandHandler.cs
│   │       └── Queries/
│   │           ├── GetAllProductsQuery.cs
│   │           └── GetAllProductsQueryHandler.cs
│   └── Services/
│       └── ProductService.cs

├── Infrastructure/
│   ├── Data/
│   │   └── ApplicationDbContext.cs
│   └── Repositories/
│       └── ProductRepository.cs

└── WebAPI/
    ├── Controllers/
    │   └── ProductsController.cs
    └── Program.cs
```

## Testing Best Practices

### Unit Testing
```csharp
// ✓ Good: Focused unit tests
[TestClass]
public class CreateProductCommandHandlerTests
{
    private CreateProductCommandHandler _handler;
    private Mock<IProductRepository> _mockRepository;

    [TestInitialize]
    public void Setup()
    {
        _mockRepository = new Mock<IProductRepository>();
        _handler = new CreateProductCommandHandler(_mockRepository.Object);
    }

    [TestMethod]
    public async Task Handle_WithValidCommand_CreatesProduct()
    {
        // Arrange
        var command = new CreateProductCommand("Laptop", "High-end laptop", 1299.99m, 50);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Id > 0);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Product>()), Times.Once);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task Handle_WithEmptyName_ThrowsException()
    {
        // Arrange
        var command = new CreateProductCommand("", "", 1299.99m, 50);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert is implicit with ExpectedException
    }
}
```

### Integration Testing
```csharp
// Test with real database context
[TestClass]
public class ProductRepositoryTests
{
    private ApplicationDbContext _context;
    private ProductRepository _repository;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new ProductRepository(_context);
    }

    [TestMethod]
    public async Task AddAsync_WithValidProduct_PersistsSuccessfully()
    {
        // Arrange
        var product = new Product { Name = "Laptop", Price = 1299.99m };

        // Act
        await _repository.AddAsync(product);
        await _repository.SaveChangesAsync();

        // Assert
        var saved = await _repository.GetByIdAsync(product.Id);
        Assert.IsNotNull(saved);
        Assert.AreEqual("Laptop", saved.Name);
    }
}
```

## Performance Optimization

### Database Queries
```csharp
// ✓ Good: Optimized queries
public async Task<List<ProductDto>> GetProductsAsync(int categoryId)
{
    /// Single round-trip to database
    /// Projection to DTOs reduces data transfer
    /// AsNoTracking for read-only queries
    return await _context.Products
        .Where(p => p.CategoryId == categoryId)
        .AsNoTracking()
        .Select(p => new ProductDto(p.Id, p.Name, p.Price, ...))
        .ToListAsync();
}

// ❌ Bad: N+1 queries
public async Task<List<ProductDto>> GetProductsAsync(int categoryId)
{
    /// First query gets products
    var products = await _context.Products
        .Where(p => p.CategoryId == categoryId)
        .ToListAsync();

    /// For each product, separate query for category - N+1 problem!
    var dtos = new List<ProductDto>();
    foreach (var product in products)
    {
        var category = await _context.Categories.FindAsync(product.CategoryId);
        dtos.Add(new ProductDto(product.Id, product.Name, category.Name, ...));
    }

    return dtos;
}
```

## Dependency Injection Configuration

```csharp
// Program.cs - Organized registration
var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);
```
