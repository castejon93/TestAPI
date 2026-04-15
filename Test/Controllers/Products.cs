using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Test.Database;
using Test.Entities;

namespace Test.Controllers
{
    [Route("api/products")]
    public class Products : Controller
    {
        private readonly ApplicationDBContext _context;

        public Products(ApplicationDBContext context) 
        {
            this._context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<List<Product>> Get()
        {
            return await _context.Products.ToListAsync();
        }
    }
}
