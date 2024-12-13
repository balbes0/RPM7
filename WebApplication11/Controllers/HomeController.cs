using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using WebApplication11.Models;

namespace WebApplication11.Controllers
{
    public class HomeController : Controller
    {
        public static string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public static bool IsPhoneNumberValid(string phoneNumber)
        {
            string pattern = @"^((8|\+7)[\- ]?)?(\(?\d{3}\)?[\- ]?)?[\d\- ]{7,10}$";
            return Regex.IsMatch(phoneNumber, pattern);
        }

        private Rpm2Context db;
        public HomeController (Rpm2Context pm2Context)
        {
            db = pm2Context;
        }

        public IActionResult EasyData()
        {
            var userid = HttpContext.Session.GetInt32("UserID");
            var user = db.Users.FirstOrDefault(u => u.IdUser == userid);

            if (user.RoleId == 1)
            {
                return View();
            }
            return NotFound();
        }

        public async Task<IActionResult> Index()
        {
            return View(await db.Catalogs.ToListAsync());
        }

        [HttpGet]
        public IActionResult Registration()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(string phoneNumber, string email, string password, string confirmPassword)
        {
            if (password != confirmPassword)
            {
                ViewBag.ErrorMessage = "Пароли не совпадают.";
                return View("Registration");
            }

            if (!IsPhoneNumberValid(phoneNumber))
            {
                ViewBag.ErrorMessage = "Неверный формат номера.";
                return View("Registration");
            }

            // Проверка на существующего пользователя
            if (db.Users.Any(u => u.Email == email || u.Phone == phoneNumber))
            {
                ViewBag.ErrorMessage = "Пользователь с таким email или номером телефона уже существует.";
                return View("Registration");
            }

            // Хэширование пароля
            string hashedPassword = ComputeSha256Hash(password);

            // Сохранение пользователя
            var user = new User
            {
                Phone = phoneNumber,
                Email = email,
                Password = hashedPassword,
                RoleId = 2
            };

            db.Users.Add(user);
            db.SaveChanges();

            // Перенаправление на страницу входа или успешной регистрации
            return RedirectToAction("Authorization");
        }

        [HttpGet]
        public IActionResult Authorization()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            // Проверка, существует ли пользователь с таким email
            var user = db.Users.FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                ViewBag.ErrorMessage = "Пользователь с таким email не найден.";
                return View("Authorization");
            }

            // Проверка пароля
            string hashedPassword = ComputeSha256Hash(password);
            if (hashedPassword != user.Password)
            {
                ViewBag.ErrorMessage = "Неверный пароль.";
                return View("Authorization");
            }

            HttpContext.Session.SetInt32("UserID", user.IdUser);
            HttpContext.Session.SetString("IsAuthenticated", "true");
            HttpContext.Session.SetInt32("RoleID", user.RoleId);

            return RedirectToAction("Profile");
        }

        public IActionResult Profile()
        {
            // Проверка на авторизацию
            var isAuthenticated = HttpContext.Session.GetString("IsAuthenticated");
            if (isAuthenticated != "true")
            {
                return RedirectToAction("Authorization");  // Если пользователь не авторизован, перенаправляем на страницу авторизации
            }

            int? isuser = HttpContext.Session.GetInt32("UserID");

            var user = db.Users.SingleOrDefault(u => u.IdUser == isuser);

            return View(user);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();  // Очищаем сессию
            return RedirectToAction("Authorization");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public async Task<IActionResult> Catalog(string filter, string search, string sort)
        {
            var catalogs = db.Catalogs.AsQueryable();
            var userid = HttpContext.Session.GetInt32("UserID");

            // Фильтрация
            if (!string.IsNullOrEmpty(filter))
            {
                catalogs = catalogs.Where(c => c.CategoryName == filter);
            }

            // Поиск
            if (!string.IsNullOrEmpty(search))
            {
                catalogs = catalogs.Where(c => c.ProductName.Contains(search) || c.Description.Contains(search));
            }

            // Сортировка
            if (!string.IsNullOrEmpty(sort))
            {
                catalogs = sort switch
                {
                    "price-asc" => catalogs.OrderBy(c => c.Price),
                    "price-desc" => catalogs.OrderByDescending(c => c.Price),
                    _ => catalogs
                };
            }

            if (userid != null)
            {
                var cartItems = await db.Carts.Where(c => c.UserId == userid).ToListAsync();

                foreach (var item in catalogs)
                {
                    item.IsInCart = cartItems.Any(c => c.ProductId == item.IdProduct);
                }
            }

            return View(await catalogs.ToListAsync());
        }


        public IActionResult AboutUs()
        {
            return View();
        }

        public async Task<IActionResult> Cart()
        {
            int? userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
            {
                return RedirectToAction("Authorization");
            }

            // Получаем все товары из корзины пользователя с навигационными свойствами (Product)
            var cartItems = await db.Carts
                .Where(c => c.UserId == userId)
                .Include(c => c.Product)  // Подключаем товар (Catalog)
                .ToListAsync();

            // Передаем корзину в представление
            return View(cartItems);
        }


        [HttpPost]
        public IActionResult AddToCart(int productId)
        {
            int? isuser = HttpContext.Session.GetInt32("UserID");
            if (isuser != null)
            {
                var productToCart = new Cart
                {
                    UserId = isuser.Value,
                    ProductId = productId
                };

                db.Carts.Add(productToCart);
                db.SaveChanges();
                return RedirectToAction("Catalog");
            }
            else
            {
                return RedirectToAction("Authorization");
            }
        }

        [HttpPost]
        public IActionResult UpdateCartQuantity(int productId, int quantity)
        {
            int? isuser = HttpContext.Session.GetInt32("UserID");
            if (isuser != null)
            {
                var cartItem = db.Carts.FirstOrDefault(c => c.UserId == isuser.Value && c.ProductId == productId);
                if (cartItem != null)
                {
                    cartItem.Quantity = quantity;  // Обновление количества товара
                    db.SaveChanges();
                }
                return RedirectToAction("Cart");
            }
            else
            {
                return RedirectToAction("Authorization");
            }
        }

        [HttpPost]
        public IActionResult RemoveCartItem(int productId, string redirectTo)
        {
            int? isuser = HttpContext.Session.GetInt32("UserID");
            if (isuser != null)
            {
                var cartItem = db.Carts.FirstOrDefault(c => c.UserId == isuser.Value && c.ProductId == productId);
                if (cartItem != null)
                {
                    db.Carts.Remove(cartItem);
                    db.SaveChanges();
                }

                // Проверяем, куда нужно перенаправить
                if (redirectTo == "Cart")
                {
                    return RedirectToAction("Cart"); // Перенаправляем на страницу корзины
                }
                else
                {
                    return RedirectToAction("Catalog"); // Перенаправляем на каталог
                }
            }
            else
            {
                return RedirectToAction("Authorization");
            }
        }

        public IActionResult Order()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
