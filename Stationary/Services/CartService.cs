using Microsoft.EntityFrameworkCore;
using Stationary.Data;
using Stationary.Models;

namespace Stationary.Services
{
    public class CartService : ICartService
    {
        private readonly ApplicationDbContext _db;

        public CartService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Cart>> GetUserCartAsync(int userId)
        {
            return await _db.Carts
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();
        }

        public async Task<Cart> AddToCartAsync(int userId, int productId, int quantity)
        {
            var existingItem = await _db.Carts
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);

            if (existingItem != null)
            {
                existingItem.Quantity = quantity;
            }
            else
            {
                existingItem = new Cart
                {
                    UserId = userId,
                    ProductId = productId,
                    Quantity = quantity
                };
                _db.Carts.Add(existingItem);
            }

            await _db.SaveChangesAsync();
            return existingItem;
        }

        public async Task<Cart> UpdateCartItemQuantityAsync(int userId, int productId, int quantity)
        {
            var cartItem = await _db.Carts
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);

            if (cartItem == null)
                throw new InvalidOperationException("Cart item not found");

            if (quantity <= 0)
            {
                _db.Carts.Remove(cartItem);
            }
            else
            {
                cartItem.Quantity = quantity;
            }

            await _db.SaveChangesAsync();
            return cartItem;
        }

        public async Task RemoveFromCartAsync(int userId, int productId)
        {
            var cartItem = await _db.Carts
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);

            if (cartItem != null)
            {
                _db.Carts.Remove(cartItem);
                await _db.SaveChangesAsync();
            }
        }

        public async Task ClearUserCartAsync(int userId)
        {
            var userCart = await _db.Carts
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (userCart.Any())
            {
                _db.Carts.RemoveRange(userCart);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<int> GetCartItemCountAsync(int userId)
        {
            return await _db.Carts
                .Where(c => c.UserId == userId)
                .SumAsync(c => c.Quantity);
        }

        public async Task<decimal> GetCartTotalAsync(int userId)
        {
            return await _db.Carts
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .SumAsync(c => c.Product.Price * c.Quantity);
        }

        public async Task<bool> ValidateCartStockAsync(int userId)
        {
            var cartItems = await _db.Carts
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            foreach (var item in cartItems)
            {
                if (item.Product == null || item.Product.StockQuantity < item.Quantity)
                    return false;
            }

            return true;
        }
    }
}