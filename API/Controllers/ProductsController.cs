using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingBasketAPI.Data;
using ShoppingBasketAPI.Models;

namespace ShoppingBasketAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController(ShoppingBasketContext context) : ControllerBase
    {
        private readonly ShoppingBasketContext _context = context;

        /// <summary>
        /// Get all active products
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            var products = await this._context.Products
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();

            return this.Ok(products);
        }

        /// <summary>
        /// Get a specific product by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await this._context.Products.FindAsync(id);

            if (product == null || !product.IsActive)
            {
                return this.NotFound();
            }

            return this.Ok(product);
        }
    }
} 
