using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Data
{
    /// <summary>
    /// Design-time factory for ApplicationDbContext.
    /// 
    /// This class is ONLY used by Entity Framework CLI tools (dotnet ef).
    /// It allows EF to create a DbContext instance when running commands like:
    /// - dotnet ef migrations add
    /// - dotnet ef database update
    /// 
    /// At runtime, the DbContext is created via dependency injection in Program.cs.
    /// This factory is needed because the CLI tools don't run the full application,
    /// so they can't access the DI container.
    /// </summary>
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        /// <summary>
        /// Creates a new instance of ApplicationDbContext for design-time operations.
        /// 
        /// EF CLI tools call this method automatically when they need a DbContext.
        /// The args parameter contains any command-line arguments passed to the CLI.
        /// </summary>
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            // Build configuration from appsettings.json
            // We need to navigate to the WebAPI project to find the config file
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../WebAPI"))
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Get connection string from configuration
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // Configure DbContext options
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            // Return new context instance
            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
