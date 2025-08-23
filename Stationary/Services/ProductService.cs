using Microsoft.EntityFrameworkCore;
using Stationary.Data;
using Stationary.Models;

namespace Stationary.Services
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _db;

        public ProductService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            return await _db.Products.ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(string category)
        {
            return await _db.Products
                .Where(p => p.Category == category)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllProductsAsync();

            return await _db.Products
                .Where(p => p.Name.Contains(searchTerm) || p.Category.Contains(searchTerm))
                .ToListAsync();
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            return await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Product> CreateProductAsync(Product product)
        {
            _db.Products.Add(product);
            await _db.SaveChangesAsync();
            return product;
        }

        public async Task<Product> UpdateProductAsync(Product product)
        {
            var existingProduct = await _db.Products.FirstOrDefaultAsync(p => p.Id == product.Id);
            if (existingProduct == null)
                throw new InvalidOperationException("Product not found");

            existingProduct.Name = product.Name;
            existingProduct.Category = product.Category;
            existingProduct.Price = product.Price;
            existingProduct.StockQuantity = product.StockQuantity;
            existingProduct.LowStockThreshold = product.LowStockThreshold;
            existingProduct.ImagePath = product.ImagePath;

            await _db.SaveChangesAsync();
            return existingProduct;
        }

        public async Task DeleteProductAsync(int id)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product != null)
            {
                _db.Products.Remove(product);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<string>> GetCategoriesAsync()
        {
            return await _db.Products
                .Select(p => p.Category)
                .Distinct()
                .ToListAsync();
        }

        public async Task<bool> IsProductInStockAsync(int productId, int quantity)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId);
            return product != null && product.StockQuantity >= quantity;
        }

        public async Task UpdateStockAsync(int productId, int quantity)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product != null)
            {
                product.StockQuantity -= quantity;
                await _db.SaveChangesAsync();
            }
        }
    }
}