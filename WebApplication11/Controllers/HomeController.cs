using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using WebApplication11.Models;

namespace WebApplication11.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HomeController : ControllerBase
    {
        private readonly Rpm2Context db;

        public HomeController(Rpm2Context context)
        {
            db = context;
        }

        [HttpGet("reviews/{id}")]
        public IActionResult GetReviews(int id)
        {
            var product = db.Catalogs.FirstOrDefault(p => p.IdProduct == id);
            if (product == null)
                return NotFound("Товар не найден.");

            var reviews = db.Reviews.Where(r => r.ProductId == id).ToList();

            foreach (var review in reviews)
            {
                var user = db.Users.FirstOrDefault(u => u.IdUser == review.UserId);
                if (user != null)
                {
                    review.FirstName = user.FirstName;
                    review.LastName = user.LastName;
                }
            }

            var model = new ProductReviewsViewModel
            {
                ProductName = product.ProductName,
                ProductId = product.IdProduct,
                Reviews = reviews
            };

            return Ok(model);
        }

        [HttpPost("reviews")]
        public IActionResult AddReview([FromBody] Review review)
        {
            if (review == null || review.UserId == 0)
                return BadRequest("Некорректные данные.");

            review.CreatedDate = DateTime.Now;
            db.Reviews.Add(review);
            db.SaveChanges();

            return Ok(new { message = "Отзыв добавлен." });
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterModel model)
        {
            if (model.Password != model.ConfirmPassword)
                return BadRequest("Пароли не совпадают.");

            if (!IsPhoneNumberValid(model.PhoneNumber))
                return BadRequest("Неверный формат номера.");

            if (db.Users.Any(u => u.Email == model.Email || u.Phone == model.PhoneNumber))
                return BadRequest("Пользователь уже существует.");

            string hashedPassword = ComputeSha256Hash(model.Password);

            var user = new User
            {
                Phone = model.PhoneNumber,
                Email = model.Email,
                Password = hashedPassword,
                RoleId = 2
            };

            db.Users.Add(user);
            db.SaveChanges();

            return Ok(new { message = "Регистрация успешна." });
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginModel model)
        {
            var user = db.Users.FirstOrDefault(u => u.Email == model.Email);
            if (user == null)
                return Unauthorized("Пользователь не найден.");

            string hashedPassword = ComputeSha256Hash(model.Password);
            if (user.Password != hashedPassword)
                return Unauthorized("Неверный пароль.");

            return Ok(new { message = "Авторизация успешна.", userId = user.IdUser, roleId = user.RoleId });
        }

        [HttpGet("catalog")]
        public async Task<IActionResult> GetCatalog([FromQuery] string? filter, [FromQuery] string? search, [FromQuery] string? sort)
        {
            var query = db.Catalogs.Include(c => c.Reviews).AsQueryable();

            if (!string.IsNullOrEmpty(filter))
                query = query.Where(c => c.CategoryName == filter);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(c => c.ProductName.Contains(search) || c.Description.Contains(search));

            query = sort switch
            {
                "price-asc" => query.OrderBy(c => c.Price),
                "price-desc" => query.OrderByDescending(c => c.Price),
                _ => query
            };

            var catalogList = await query.ToListAsync();

            foreach (var item in catalogList)
            {
                var ratedReviews = item.Reviews.Where(r => r.Rating.HasValue).ToList();
                item.ReviewCount = ratedReviews.Count;
                item.AverageRating = ratedReviews.Any()
                    ? ratedReviews.Average(r => r.Rating.Value)
                    : 0;
            }

            return Ok(catalogList);
        }

        [HttpPost("cart/add")]
        public IActionResult AddToCart([FromBody] Cart item)
        {
            db.Carts.Add(item);
            db.SaveChanges();
            return Ok(new { message = "Товар добавлен в корзину." });
        }

        [HttpPut("cart/update")]
        public IActionResult UpdateCartQuantity([FromBody] Cart item)
        {
            var cartItem = db.Carts.FirstOrDefault(c => c.UserId == item.UserId && c.ProductId == item.ProductId);
            if (cartItem == null)
                return NotFound();

            cartItem.Quantity = item.Quantity;
            db.SaveChanges();

            return Ok(new { message = "Количество обновлено." });
        }

        [HttpDelete("cart/remove")]
        public IActionResult RemoveCartItem([FromQuery] int userId, [FromQuery] int productId)
        {
            var item = db.Carts.FirstOrDefault(c => c.UserId == userId && c.ProductId == productId);
            if (item == null)
                return NotFound();

            db.Carts.Remove(item);
            db.SaveChanges();

            return Ok(new { message = "Товар удален из корзины." });
        }

        [HttpGet("cart/{userId}")]
        public async Task<IActionResult> GetCart(int userId)
        {
            var items = await db.Carts
                .Where(c => c.UserId == userId)
                .Include(c => c.Product)
                .ToListAsync();

            return Ok(items);
        }

        // Вспомогательные методы
        public static string ComputeSha256Hash(string rawData)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }

        public static bool IsPhoneNumberValid(string phoneNumber)
        {
            string pattern = @"^((8|\+7)[\- ]?)?(\(?\d{3}\)?[\- ]?)?[\d\- ]{7,10}$";
            return Regex.IsMatch(phoneNumber, pattern);
        }
    }

    // Модели для регистрации и входа
    public class RegisterModel
    {
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
    }

    public class LoginModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
