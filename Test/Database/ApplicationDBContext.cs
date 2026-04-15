using Microsoft.EntityFrameworkCore;
using Test.Entities;

namespace Test.Database
{
    public class ApplicationDBContext : DbContext
    {
        public ApplicationDBContext (DbContextOptions options) : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
    }
}
