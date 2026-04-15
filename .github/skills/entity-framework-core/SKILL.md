---
name: entity-framework-core
description: "Use when working with Entity Framework Core, database contexts, migrations, and LINQ queries"
---

# Entity Framework Core Skill Guide

## Overview
Entity Framework Core is a modern Object-Relational Mapper (ORM) for .NET that enables database-agnostic data access.

## Core Concepts

### DbContext
- Manages entity instances
- Tracks changes to entities
- Persists changes to database
- Provides querying interface (LINQ)

### DbSet
- Represents a collection of entities
- Enables LINQ queries
- Supports Add, Remove, Update operations

### Migrations
- Version-control for database schema
- Enable reproducible database changes
- Track schema history

## Implementation Guidelines

### DbContext Setup
```csharp
// Infrastructure/Data/ApplicationDbContext.cs
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DbSets for each entity
    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure entities
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);
            
            entity.Property(e => e.Price)
                .HasPrecision(10, 2);

            // Configure relationships
            entity.HasOne(e => e.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
```

### Repository Pattern
```csharp
// Domain/Interfaces/IProductRepository.cs
public interface IProductRepository
{
    Task<Product> GetByIdAsync(int id);
    Task<List<Product>> GetAllAsync();
    Task AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(int id);
    Task SaveChangesAsync();
}

// Infrastructure/Repositories/ProductRepository.cs
public class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _context;

    public ProductRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves a product by its ID
    /// 
    /// Parameters:
    /// - id: Product identifier
    /// 
    /// Returns:
    /// - Product entity if found, null otherwise
    /// 
    /// Note: Uses FindAsync for best performance (checks change tracker first)
    /// </summary>
    public async Task<Product> GetByIdAsync(int id)
    {
        return await _context.Products.FindAsync(id);
    }

    /// <summary>
    /// Retrieves all products
    /// 
    /// Returns:
    /// - List of all Product entities
    /// 
    /// Performance Considerations:
    /// - For large datasets, consider pagination
    /// - Use AsNoTracking() for read-only queries
    /// </summary>
    public async Task<List<Product>> GetAllAsync()
    {
        return await _context.Products
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// Adds a new product
    /// 
    /// Note: Does not save immediately, must call SaveChangesAsync()
    /// </summary>
    public async Task AddAsync(Product product)
    {
        await _context.Products.AddAsync(product);
    }

    /// <summary>
    /// Updates an existing product
    /// 
    /// Note: Entity must be tracked by context
    /// </summary>
    public async Task UpdateAsync(Product product)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Saves all pending changes to database
    /// 
    /// Called after Add, Update, or Delete operations
    /// </summary>
    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
```

### Querying with LINQ
```csharp
// Basic queries
var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
var allProducts = await _context.Products.ToListAsync();

// Filtered queries
var expensive = await _context.Products
    .Where(p => p.Price > 100)
    .ToListAsync();

// Ordering
var sorted = await _context.Products
    .OrderBy(p => p.Name)
    .ToListAsync();

// Pagination
var page = await _context.Products
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();

// Eager loading (prevent N+1)
var productsWithCategory = await _context.Products
    .Include(p => p.Category)
    .ToListAsync();

// Projection to DTOs
var dtos = await _context.Products
    .Select(p => new ProductDto(p.Id, p.Name, ...))
    .ToListAsync();

// AsNoTracking for read-only queries (better performance)
var readonly = await _context.Products
    .AsNoTracking()
    .ToListAsync();
```

## Migrations

### Creating Migrations
```powershell
# Add a new migration
dotnet ef migrations add InitialCreate --project Infrastructure

# Apply migrations to database
dotnet ef database update --project Infrastructure

# Remove last migration
dotnet ef migrations remove
```

### Migration File Structure
```csharp
public partial class CreateProductsTable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Products",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(maxLength: 200, nullable: false),
                Price = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                CreatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Products", x => x.Id);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Products");
    }
}
```

## Best Practices

1. **Use FindAsync for Primary Keys**: Faster than Where() queries
2. **AsNoTracking for Read-Only**: Improves performance when you don't need tracking
3. **Include Related Data**: Use Include() to avoid N+1 queries
4. **Projection to DTOs**: Project in LINQ before materializing
5. **Keep DbContext Focused**: One DbContext per domain model
6. **Use Async Methods**: Always use async for I/O operations
7. **Handle Concurrency**: Use ConcurrencyToken for optimistic locking

## Anti-Patterns to Avoid

❌ Querying in a loop (N+1 problem)
❌ Loading all data then filtering in memory
❌ Tracking entities unnecessarily
❌ Large DbContext with too many entities
❌ Not using migrations for schema changes
❌ Direct SQL queries in business logic

## Configuration

### Connection String (appsettings.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=ProductDb;Trusted_Connection=true;TrustServerCertificate=true;"
  }
}
```

### Program.cs Setup
```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);
```
