using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stationary.Data;
using Stationary.Models;
using Stationary.Services;

namespace Stationary.Controllers
{
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IProductService _productService;
        private readonly ICartService _cartService;

        public UserController(ApplicationDbContext db, IProductService productService, ICartService cartService)
        {
            _db = db;
            _productService = productService;
            _cartService = cartService;
        }

        public IActionResult Login()
        {
            return View();
        }



        // Product List with Search
        public async Task<IActionResult> Index(string search, string category, string stockFilter = "available", int page = 1, int pageSize = 12)
        {
            if (HttpContext.Session.GetString("Role") != "User")
                return RedirectToAction("Login", "User");

                            IEnumerable<Product> products;

                // Apply stock filter
                switch (stockFilter?.ToLower())
                {
                    case "all":
                        products = await _productService.GetAvailableProductsAsync(true);
                        break;
                    case "outofstock":
                        products = await _productService.GetOutOfStockProductsAsync();
                        break;
                    case "lowstock":
                        products = await _productService.GetLowStockProductsAsync();
                        break;
                    default:
                        products = await _productService.GetAvailableProductsAsync(false);
                        break;
                }

                            // Apply search filter
                if (!string.IsNullOrEmpty(search))
                    products = products.Where(p => p.Name.Contains(search, StringComparison.OrdinalIgnoreCase));

                // Apply category filter
                if (!string.IsNullOrEmpty(category))
                    products = products.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase));

                // Get total count for pagination
                var totalProducts = products.Count();
                var totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);

                // Apply pagination
                var pagedProducts = products
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Get unique categories for filter dropdown
                var categories = await _productService.GetCategoriesAsync();

                            ViewBag.Search = search;
                ViewBag.Category = category;
                ViewBag.StockFilter = stockFilter;
                ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Categories = categories;
            ViewBag.PageSize = pageSize;

            return View(pagedProducts);
        }


        [HttpPost]
        public async Task<JsonResult> AddToCart(int id, int quantity = 1)
        {
            try
            {
                var username = HttpContext.Session.GetString("Username");
                if (string.IsNullOrEmpty(username))
                    return Json(new { success = false, message = "Please login first.", redirect = true });

                var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                    return Json(new { success = false, message = "User not found.", redirect = true });

                var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
                if (product == null)
                    return Json(new { success = false, message = "Product not found." });

                // Validate quantity
                if (quantity <= 0)
                    return Json(new { success = false, message = "Quantity must be greater than 0." });

                // Check if product is out of stock
                if (product.IsOutOfStock)
                    return Json(new { success = false, message = "This product is currently out of stock." });

                // Check stock availability
                if (product.StockQuantity < quantity)
                    return Json(new { success = false, message = $"Only {product.StockQuantity} items available in stock." });

                var cartItem = await _db.Carts.FirstOrDefaultAsync(c => c.ProductId == id && c.UserId == user.Id);

                if (cartItem == null)
                {
                    // New product → add with selected quantity
                    _db.Carts.Add(new Cart { UserId = user.Id, ProductId = id, Quantity = quantity });
                }
                else
                {
                    // Product already in cart → update quantity
                    cartItem.Quantity = quantity;
                }

                await _db.SaveChangesAsync();

                // Count total quantity across all items
                var cartCount = await _db.Carts.Where(c => c.UserId == user.Id).SumAsync(c => c.Quantity);

                return Json(new { success = true, count = cartCount, message = "Product added to cart successfully!" });
            }
            catch (Exception ex)
            {
                // Log the exception (in production, use proper logging)
                return Json(new { success = false, message = "An error occurred while adding to cart." });
            }
        }


        [HttpGet]
        public async Task<IActionResult> Cart()
        {
            try
            {
                var username = HttpContext.Session.GetString("Username");
                if (string.IsNullOrEmpty(username))
                    return RedirectToAction("Login", "User");

                var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                    return RedirectToAction("Login", "User");

                var cart = await _db.Carts
                    .Include(c => c.Product)
                    .Where(c => c.UserId == user.Id)
                    .ToListAsync();

                // Calculate total price
                var totalPrice = cart.Sum(c => c.Product.Price * c.Quantity);
                ViewBag.TotalPrice = totalPrice;
                ViewBag.ItemCount = cart.Count;

                return View(cart);
            }
            catch (Exception ex)
            {
                // Log the exception (in production, use proper logging)
                return RedirectToAction("Error", "Home");
            }
        }


        [HttpPost]
        public async Task<JsonResult> RemoveFromCart(int id)
        {
            try
            {
                var username = HttpContext.Session.GetString("Username");
                if (string.IsNullOrEmpty(username))
                    return Json(new { success = false, message = "Please login first." });

                var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                    return Json(new { success = false, message = "User not found." });

                var cartItem = await _db.Carts.FirstOrDefaultAsync(c => c.ProductId == id && c.UserId == user.Id);
                if (cartItem != null)
                {
                    _db.Carts.Remove(cartItem);
                    await _db.SaveChangesAsync();
                }

                // Get updated cart count
                var cartCount = await _db.Carts.Where(c => c.UserId == user.Id).SumAsync(c => c.Quantity);

                return Json(new { success = true, count = cartCount, message = "Item removed from cart successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while removing item from cart." });
            }
        }

        [HttpPost]
        public async Task<JsonResult> UpdateCartQuantity(int id, int quantity)
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return Json(new { success = false, message = "Please login first.", redirect = true });

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                return Json(new { success = false, message = "User not found.", redirect = true });

            var cartItem = await _db.Carts.FirstOrDefaultAsync(c => c.ProductId == id && c.UserId == user.Id);
            if (cartItem == null)
                return Json(new { success = false, message = "Cart item not found." });

            // ✅ Update exact quantity (don’t add again)
            cartItem.Quantity = quantity;
            _db.SaveChanges();

            // return updated cart count
            var cartCount = _db.Carts.Where(c => c.UserId == user.Id).Sum(c => c.Quantity);

            return Json(new { success = true, count = cartCount });
        }

        [HttpGet]
        public async Task<JsonResult> GetCartCount()
        {
            try
            {
                var username = HttpContext.Session.GetString("Username");
                if (string.IsNullOrEmpty(username))
                    return Json(new { count = 0 });

                var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                    return Json(new { count = 0 });

                var cartCount = await _db.Carts.Where(c => c.UserId == user.Id).SumAsync(c => c.Quantity);
                return Json(new { count = cartCount });
            }
            catch (Exception ex)
            {
                return Json(new { count = 0 });
            }
        }


        // Checkout
        public async Task<IActionResult> Checkout()
        {
            try
            {
                var username = HttpContext.Session.GetString("Username");
                if (string.IsNullOrEmpty(username))
                    return RedirectToAction("Login", "User");

                var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                    return RedirectToAction("Login", "User");

                var cart = await _db.Carts
                                  .Include(c => c.Product)
                                  .Where(c => c.UserId == user.Id)
                                  .ToListAsync();

            if (!cart.Any())
                return RedirectToAction("Cart");

            // Stock validation
            foreach (var item in cart)
            {
                if (item.Product == null)
                    return RedirectToAction("Cart");

                if (item.Product.StockQuantity < item.Quantity)
                {
                    TempData["Error"] = $"Insufficient stock for {item.Product.Name}. Available: {item.Product.StockQuantity}";
                    return RedirectToAction("Cart");
                }
            }

            // Deduct stock
            foreach (var item in cart)
            {
                item.Product.StockQuantity -= item.Quantity;
            }

            var total = cart.Sum(c => c.Product.Price * c.Quantity);

            var order = new Order
            {
                UserId = user.Id,
                TotalAmount = total,
                Date = DateTime.Now,
                OrderItems = cart.Select(c => new OrderItem
                {
                    ProductId = c.ProductId,
                    Quantity = c.Quantity,
                    ProductName = c.Product.Name,   // 👈 save product name
                    Price = c.Product.Price
                }).ToList()
            };

            _db.Orders.Add(order);
            _db.Carts.RemoveRange(cart);
            await _db.SaveChangesAsync();

            return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Log the exception (in production, use proper logging)
                TempData["Error"] = "An error occurred during checkout. Please try again.";
                return RedirectToAction("Cart");
            }
        }

    }

}
