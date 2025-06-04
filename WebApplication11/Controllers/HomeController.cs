using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using WebApplication11.Models;
using WebApplication11.Models.Helpers;

namespace WebApplication11.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApiService _apiService;

        public HomeController(ApiService apiService)
        {
            _apiService = apiService;
        }

        // Главная страница - список товаров
        public async Task<IActionResult> Index()
        {
            try
            {
                var catalogs = await _apiService.GetCatalogsAsync();
                return View(catalogs);
            }
            catch (HttpRequestException ex)
            {
                ViewBag.ErrorMessage = "Ошибка при загрузке каталога: " + ex.Message;
                return View(new List<CatalogDto>());
            }
        }

        // Каталог с фильтрацией, поиском и сортировкой
        public async Task<IActionResult> Catalog(string? filter, string? search, string? sort)
        {
            try
            {
                var catalogList = await _apiService.GetCatalogAsync(filter, search, sort);
                ViewData["Search"] = search; 
                return View(catalogList);
            }
            catch (HttpRequestException ex)
            {
                ViewBag.ErrorMessage = "Ошибка при загрузке каталога: " + ex.Message;
                return View(new List<CatalogDto>());
            }
        }

        // Отзывы для товара
        public async Task<IActionResult> Reviews(int id)
        {
            var reviews = await _apiService.GetReviewsAsync(id);
            return View(reviews);
        }

        // Добавление отзыва
        [HttpPost]
        public async Task<IActionResult> AddReview(AddReviewRequest request)
        {
            var success = await _apiService.AddReviewAsync(request);
            if (success)
            {
                return RedirectToAction("Reviews", new { id = request.ProductId });
            }
            return BadRequest("Не удалось добавить отзыв.");
        }

        // Форма регистрации
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            try
            {
                var success = await _apiService.RegisterAsync(request);
                if (success)
                {
                    return RedirectToAction("Login");
                }
                ViewBag.ErrorMessage = "Не удалось зарегистрироваться. Проверьте введенные данные.";
                return View(request);
            }
            catch (HttpRequestException ex)
            {
                ViewBag.ErrorMessage = "Ошибка при регистрации: " + ex.Message;
                return View(request);
            }
        }

        // Форма входа
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var user = await _apiService.LoginAsync(request);
            if (user != null)
            {
                HttpContext.Session.SetInt32("UserID", user.IdUser);
                HttpContext.Session.SetString("IsAuthenticated", "true");
                HttpContext.Session.SetInt32("RoleID", user.RoleId);
                return RedirectToAction("Index");
            }
            return View(request);
        }

        // Профиль пользователя
        public async Task<IActionResult> Profile()
        {
            try
            {
                var user = await _apiService.GetProfileAsync();
                if (user == null)
                {
                    ViewBag.ErrorMessage = "Не удалось загрузить данные профиля.";
                    return View();
                }
                return View(user);
            }
            catch (HttpRequestException ex)
            {
                ViewBag.ErrorMessage = "Ошибка при загрузке профиля: " + ex.Message;
                return View();
            }
        }

        // Выход
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _apiService.LogoutAsync();
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }

        // EasyData (только для администраторов)
        public async Task<IActionResult> EasyData()
        {
            var result = await _apiService.GetEasyDataAsync();
            return View(model: result);
        }

        // Корзина
        public async Task<IActionResult> Cart()
        {

            var cartItems = await _apiService.GetCartAsync();
            return View(cartItems);
        }

        // Добавление в корзину
        [HttpPost]
        public async Task<IActionResult> AddToCart(AddToCartRequest request)
        {
            var success = await _apiService.AddToCartAsync(request);
            if (success)
            {
                return RedirectToAction("Cart");
            }
            return BadRequest("Не удалось добавить товар в корзину.");
        }

        // Обновление количества в корзине
        [HttpPost]
        public async Task<IActionResult> UpdateCartQuantity(UpdateCartRequest request)
        {
            var success = await _apiService.UpdateCartQuantityAsync(request);
            if (success)
            {
                return RedirectToAction("Cart");
            }
            return BadRequest("Не удалось обновить количество.");
        }

        // Удаление из корзины
        [HttpPost]
        public async Task<IActionResult> RemoveCartItem(int productId)
        {
            var success = await _apiService.RemoveCartItemAsync(productId);
            if (success)
            {
                return RedirectToAction("Cart");
            }
            return BadRequest("Не удалось удалить товар из корзины.");
        }

        // Оформление заказа
        public IActionResult Order()
        {
            return View();
        }

        // О нас
        public async Task<IActionResult> AboutUs()
        {
            var info = await _apiService.GetAboutUsAsync();
            return View(info);
        }

        // Политика конфиденциальности
        public async Task<IActionResult> Privacy()
        {
            var policy = await _apiService.GetPrivacyAsync();
            return View(model: policy);
        }
    }
}