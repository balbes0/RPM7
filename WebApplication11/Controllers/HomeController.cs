using Microsoft.AspNetCore.Mvc;
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

        private bool IsAuthenticated => HttpContext.Session.GetString("IsAuthenticated") == "true";

        private IActionResult RedirectToLoginIfUnauthorized(string returnUrl = null)
        {
            if (!IsAuthenticated)
            {
                var loginUrl = Url.Action("Login", "Home", new { returnUrl = returnUrl ?? Request.Path + Request.QueryString });
                return Redirect(loginUrl);
            }
            return null;
        }

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

        public async Task<IActionResult> Reviews(int id)
        {
            try
            {
                var reviews = await _apiService.GetReviewsAsync(id);
                return View(reviews);
            }
            catch (HttpRequestException ex)
            {
                ViewBag.ErrorMessage = "Ошибка при загрузке отзывов: " + ex.Message;
                return View(new ProductReviewsViewModelDto());
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddReview(AddReviewRequest request)
        {
            var redirect = RedirectToLoginIfUnauthorized(Url.Action("Reviews", new { id = request.ProductId }));
            if (redirect != null) return redirect;

            var (success, errorMessage) = await _apiService.AddReviewAsync(request);
            if (success)
            {
                return RedirectToAction("Reviews", new { id = request.ProductId });
            }
            ViewBag.ErrorMessage = errorMessage;
            var reviews = await _apiService.GetReviewsAsync(request.ProductId);
            return View("Reviews", reviews);
        }

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

        public IActionResult Login(string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginRequest request, string returnUrl = null)
        {
            var (user, errorMessage) = await _apiService.LoginAsync(request);
            if (user != null)
            {
                return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
                    ? Redirect(returnUrl)
                    : RedirectToAction("Index");
            }
            ViewBag.ErrorMessage = errorMessage;
            ViewBag.ReturnUrl = returnUrl;
            return View(request);
        }

        public async Task<IActionResult> Profile()
        {
            var redirect = RedirectToLoginIfUnauthorized();
            if (redirect != null) return redirect;

            try
            {
                var (profile, errorMessage) = await _apiService.GetProfileAsync();
                if (profile == null)
                {
                    ViewBag.ErrorMessage = errorMessage;
                    return View();
                }
                return View(profile);
            }
            catch (HttpRequestException ex)
            {
                ViewBag.ErrorMessage = "Ошибка при загрузке профиля: " + ex.Message;
                return View();
            }
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _apiService.LogoutAsync();
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> EasyData()
        {
            var redirect = RedirectToLoginIfUnauthorized();
            if (redirect != null) return redirect;

            var roleId = HttpContext.Session.GetInt32("RoleID");
            if (roleId != 1)
            {
                return Forbid();
            }

            try
            {
                var (data, errorMessage) = await _apiService.GetEasyDataAsync();
                if (data == null)
                {
                    ViewBag.ErrorMessage = errorMessage;
                    return View();
                }
                ViewBag.EasyData = data;
                return View();
            }
            catch (HttpRequestException ex)
            {
                ViewBag.ErrorMessage = "Ошибка при загрузке EasyData: " + ex.Message;
                return View();
            }
        }

        public async Task<IActionResult> Cart()
        {
            var redirect = RedirectToLoginIfUnauthorized();
            if (redirect != null) return redirect;

            try
            {
                var (cartItems, errorMessage) = await _apiService.GetCartAsync();
                if (cartItems == null)
                {
                    ViewBag.ErrorMessage = errorMessage;
                    return View(new List<CartItemDto>());
                }
                return View(cartItems);
            }
            catch (HttpRequestException ex)
            {
                ViewBag.ErrorMessage = "Ошибка при загрузке корзины: " + ex.Message;
                return View(new List<CartItemDto>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(AddToCartRequest request)
        {
            var redirect = RedirectToLoginIfUnauthorized(Url.Action("Cart"));
            if (redirect != null) return redirect;

            var (success, errorMessage) = await _apiService.AddToCartAsync(request);
            if (success)
            {
                return RedirectToAction("Cart");
            }
            ViewBag.ErrorMessage = errorMessage;
            return RedirectToAction("Catalog");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCartQuantity(UpdateCartRequest request)
        {
            var redirect = RedirectToLoginIfUnauthorized();
            if (redirect != null) return redirect;

            var (success, errorMessage) = await _apiService.UpdateCartQuantityAsync(request);
            if (success)
            {
                return RedirectToAction("Cart");
            }
            ViewBag.ErrorMessage = errorMessage;
            return RedirectToAction("Cart");
        }

        [HttpPost]
        public async Task<IActionResult> RemoveCartItem(int productId)
        {
            var redirect = RedirectToLoginIfUnauthorized();
            if (redirect != null) return redirect;

            var (success, errorMessage) = await _apiService.RemoveCartItemAsync(productId);
            if (success)
            {
                return RedirectToAction("Cart");
            }
            ViewBag.ErrorMessage = errorMessage;
            return RedirectToAction("Cart");
        }

        public async Task<IActionResult> Order()
        {
            var redirect = RedirectToLoginIfUnauthorized();
            if (redirect != null) return redirect;

            try
            {
                var (success, errorMessage) = await _apiService.PlaceOrderAsync();
                if (success)
                {
                    return RedirectToAction("Cart"); // Adjust as needed
                }
                ViewBag.ErrorMessage = errorMessage;
                return View();
            }
            catch (HttpRequestException ex)
            {
                ViewBag.ErrorMessage = "Ошибка при оформлении заказа: " + ex.Message;
                return View();
            }
        }

        public async Task<IActionResult> AboutUs()
        {
            try
            {
                var aboutUs = await _apiService.GetAboutUsAsync();
                return View(aboutUs);
            }
            catch (HttpRequestException ex)
            {
                ViewBag.ErrorMessage = "Ошибка при загрузке информации: " + ex.Message;
                return View();
            }
        }

        public async Task<IActionResult> Privacy()
        {
            try
            {
                var policy = await _apiService.GetPrivacyAsync();
                return View(model: policy);
            }
            catch (HttpRequestException ex)
            {
                ViewBag.ErrorMessage = "Ошибка при загрузке политики: " + ex.Message;
                return View();
            }
        }
    }
}
