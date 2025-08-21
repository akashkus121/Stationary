using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stationary.Data;
using Stationary.Models;

namespace Stationary.Controllers
{
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _db;

        public UserController(ApplicationDbContext db)  // ✅ DI injects options
        {
            _db = db;
        }

        public IActionResult Login()
        {
            return View();
        }




        // Product List with Search
        // Product List with Search
        public IActionResult Index(string search, string category)
        {
            if (HttpContext.Session.GetString("Role") != "User")
                return RedirectToAction("Login", "User");

            var username = HttpContext.Session.GetString("Username");
            if (!string.IsNullOrEmpty(username))
            {
                var user = _db.Users.FirstOrDefault(u => u.Username == username);
                if (user != null)
                {
                    // 🔴 Clear the cart from DB every time Index refreshes
                    var userCart = _db.Carts.Where(c => c.UserId == user.Id).ToList();
                    if (userCart.Any())
                    {
                        _db.Carts.RemoveRange(userCart);
                        _db.SaveChanges();
                    }
                }
            }

            var products = _db.Products.AsQueryable();

            if (!string.IsNullOrEmpty(search))
                products = products.Where(p => p.Name.Contains(search));

            if (!string.IsNullOrEmpty(category))
                products = products.Where(p => p.Category == category);

            return View(products.ToList());
        }


        [HttpPost]
        public JsonResult AddToCart(int id, int quantity = 1)
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return Json(new { success = false, message = "Please login first.", redirect = true });

            var user = _db.Users.FirstOrDefault(u => u.Username == username);
            if (user == null)
                return Json(new { success = false, message = "User not found.", redirect = true });

            var product = _db.Products.FirstOrDefault(p => p.Id == id);
            if (product == null)
                return Json(new { success = false, message = "Product not found." });

            var cartItem = _db.Carts.FirstOrDefault(c => c.ProductId == id && c.UserId == user.Id);

            if (cartItem == null)
            {
                // new product → add with selected quantity
                _db.Carts.Add(new Cart { UserId = user.Id, ProductId = id, Quantity = quantity });
            }
            else
            {
                // product already in cart → just update quantity
                cartItem.Quantity = quantity;
            }

            _db.SaveChanges();

            // ✅ count distinct products
            var cartCount = _db.Carts.Where(c => c.UserId == user.Id).Count();

            return Json(new { success = true, count = cartCount });
        }


        [HttpGet]
        public ActionResult Cart()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Login", "User");

            var user = _db.Users.FirstOrDefault(u => u.Username == username);
            if (user == null)
                return RedirectToAction("Login", "User");

            var cart = _db.Carts
                .Include(c => c.Product)
                .Where(c => c.UserId == user.Id)
                .ToList();

            return View(cart);
        }


        [HttpPost]
        public JsonResult RemoveFromCart(int id)
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return Json(new { success = false });

            var user = _db.Users.FirstOrDefault(u => u.Username == username);
            if (user == null)
                return Json(new { success = false });

            var cartItem = _db.Carts.FirstOrDefault(c => c.ProductId == id && c.UserId == user.Id);
            if (cartItem != null)
            {
                _db.Carts.Remove(cartItem);
                _db.SaveChanges();
            }

            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult UpdateCartQuantity(int id, int quantity)
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return Json(new { success = false, message = "Please login first.", redirect = true });

            var user = _db.Users.FirstOrDefault(u => u.Username == username);
            if (user == null)
                return Json(new { success = false, message = "User not found.", redirect = true });

            var cartItem = _db.Carts.FirstOrDefault(c => c.ProductId == id && c.UserId == user.Id);
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
        public JsonResult GetCartCount()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return Json(new { count = 0 });

            var user = _db.Users.FirstOrDefault(u => u.Username == username);
            if (user == null)
                return Json(new { count = 0 });

            var cartCount = _db.Carts.Where(c => c.UserId == user.Id).Sum(c => c.Quantity);
            return Json(new { count = cartCount });
        }


        // Checkout
        public ActionResult Checkout()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Login", "User");

            var user = _db.Users.FirstOrDefault(u => u.Username == username);
            if (user == null)
                return RedirectToAction("Login", "User");

            var cart = _db.Carts
                          .Include(c => c.Product)
                          .Where(c => c.UserId == user.Id)
                          .ToList();

            if (!cart.Any())
                return RedirectToAction("Cart");

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
            _db.SaveChanges();

            return RedirectToAction("Index");
        }

    }

}
