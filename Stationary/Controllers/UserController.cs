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

            var cartItem = _db.Carts.FirstOrDefault(c => c.ProductId == id && c.UserId == user.Id);
            if (cartItem == null)
                _db.Carts.Add(new Cart { UserId = user.Id, ProductId = id, Quantity = quantity });
            else
                cartItem.Quantity += quantity;
                

            _db.SaveChanges();
            return Json(new { success = true });
        }




        public ActionResult Cart()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Login", "User");

            var user = _db.Users.FirstOrDefault(u => u.Username == username);
            if (user == null)
                return RedirectToAction("Login", "User");

            var userId = user.Id;

            var cart = _db.Carts
                          .Include(c => c.Product) // EF Core style
                          .Where(c => c.UserId == userId)
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



        // Checkout
        public ActionResult Checkout()
        {
            var userId = 1;
            var cart = _db.Carts.Where(c => c.UserId == userId).ToList();
            var total = cart.Sum(c => c.Product.Price * c.Quantity);

            _db.Orders.Add(new Order { UserId = userId, TotalAmount = total, Date = DateTime.Now });
            _db.Carts.RemoveRange(cart);
           _db.SaveChanges();

            return RedirectToAction("OrderSuccess");
        }
    }

}
