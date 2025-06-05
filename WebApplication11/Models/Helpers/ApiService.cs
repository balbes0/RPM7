using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace WebApplication11.Models.Helpers
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _apiBaseUrl;

        public ApiService(HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _apiBaseUrl = configuration["ApiBaseUrl"] ?? "https://localhost:7291";
        }

        private void AddAuthorizationHeader()
        {
            var token = _httpContextAccessor.HttpContext?.Request.Cookies["JwtToken"];
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        public async Task<List<CatalogDto>> GetCatalogsAsync()
        {
            AddAuthorizationHeader();
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/Home");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<CatalogDto>>() ?? new List<CatalogDto>();
        }

        public async Task<List<CatalogDto>> GetCatalogAsync(string? filter, string? search, string? sort)
        {
            AddAuthorizationHeader();
            var query = $"?filter={filter}&search={search}&sort={sort}";
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/Home/Catalog{query}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<CatalogDto>>() ?? new List<CatalogDto>();
        }

        public async Task<ProductReviewsViewModelDto> GetReviewsAsync(int id)
        {
            AddAuthorizationHeader();
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/Home/Reviews/{id}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ProductReviewsViewModelDto>() ?? new ProductReviewsViewModelDto();
        }

        public async Task<(bool Success, string ErrorMessage)> AddReviewAsync(AddReviewRequest request)
        {
            AddAuthorizationHeader();
            var response = await _httpClient.PostAsJsonAsync($"{_apiBaseUrl}/api/Home/AddReview", request);
            if (response.IsSuccessStatusCode)
                return (true, string.Empty);
            return (false, response.StatusCode == System.Net.HttpStatusCode.Unauthorized ? "Не авторизован." : "Не удалось добавить отзыв.");
        }

        public async Task<bool> RegisterAsync(RegisterRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync($"{_apiBaseUrl}/api/Home/Register", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<(LoginResponse? User, string ErrorMessage)> LoginAsync(LoginRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync($"{_apiBaseUrl}/api/Home/Login", request);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var user = JsonSerializer.Deserialize<LoginResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (user?.Token != null)
                {
                    _httpContextAccessor.HttpContext?.Response.Cookies.Append("JwtToken", user.Token, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTimeOffset.Now.AddMinutes(30)
                    });
                    _httpContextAccessor.HttpContext?.Session.SetInt32("UserID", user.IdUser);
                    _httpContextAccessor.HttpContext?.Session.SetInt32("RoleID", user.RoleId);
                    _httpContextAccessor.HttpContext?.Session.SetString("IsAuthenticated", "true");
                    return (user, string.Empty);
                }
                return (null, "Ошибка получения токена.");
            }
            return (null, "Неверный email или пароль.");
        }

        public async Task<(UserProfileDto? Profile, string ErrorMessage)> GetProfileAsync()
        {
            AddAuthorizationHeader();
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/Home/Profile");
            if (response.IsSuccessStatusCode)
            {
                var profile = await response.Content.ReadFromJsonAsync<UserProfileDto>();
                return (profile, string.Empty);
            }
            return (null, response.StatusCode == System.Net.HttpStatusCode.Unauthorized ? "Не авторизован." : "Не удалось загрузить профиль.");
        }

        public async Task<bool> LogoutAsync()
        {
            AddAuthorizationHeader();
            var response = await _httpClient.PostAsync($"{_apiBaseUrl}/api/Home/Logout", null);
            _httpContextAccessor.HttpContext?.Response.Cookies.Delete("JwtToken");
            _httpContextAccessor.HttpContext?.Session.Clear();
            return response.IsSuccessStatusCode;
        }

        public async Task<(string? Data, string ErrorMessage)> GetEasyDataAsync()
        {
            AddAuthorizationHeader();
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/Home/EasyData");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                return (result, string.Empty);
            }
            return (null, response.StatusCode == System.Net.HttpStatusCode.Unauthorized ? "Не авторизован." : "Доступ запрещён.");
        }

        public async Task<(IEnumerable<CartItemDto>? Cart, string ErrorMessage)> GetCartAsync()
        {
            AddAuthorizationHeader();
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/Home/Cart");
            if (response.IsSuccessStatusCode)
            {
                var cartItems = await response.Content.ReadFromJsonAsync<IEnumerable<CartItemDto>>();
                return (cartItems, string.Empty);
            }
            return (null, response.StatusCode == System.Net.HttpStatusCode.Unauthorized ? "Не авторизован." : "Ошибка загрузки корзины.");
        }

        public async Task<(bool Success, string ErrorMessage)> AddToCartAsync(AddToCartRequest request)
        {
            AddAuthorizationHeader();
            var response = await _httpClient.PostAsJsonAsync($"{_apiBaseUrl}/api/Home/AddToCart", request);
            if (response.IsSuccessStatusCode)
                return (true, string.Empty);
            return (false, response.StatusCode == System.Net.HttpStatusCode.Unauthorized ? "Не авторизован." : "Не удалось добавить товар в корзину.");
        }

        public async Task<(bool Success, string ErrorMessage)> UpdateCartQuantityAsync(UpdateCartRequest request)
        {
            AddAuthorizationHeader();
            var response = await _httpClient.PutAsJsonAsync($"{_apiBaseUrl}/api/Home/UpdateCartQuantity", request);
            if (response.IsSuccessStatusCode)
                return (true, string.Empty);
            return (false, response.StatusCode == System.Net.HttpStatusCode.Unauthorized ? "Не авторизован." : "Не удалось обновить количество.");
        }

        public async Task<(bool Success, string ErrorMessage)> RemoveCartItemAsync(int productId)
        {
            AddAuthorizationHeader();
            var response = await _httpClient.DeleteAsync($"{_apiBaseUrl}/api/Home/RemoveCartItem/{productId}");
            if (response.IsSuccessStatusCode)
                return (true, string.Empty);
            return (false, response.StatusCode == System.Net.HttpStatusCode.Unauthorized ? "Не авторизован." : "Не удалось удалить товар из корзины.");
        }

        public async Task<(bool Success, string ErrorMessage)> PlaceOrderAsync()
        {
            AddAuthorizationHeader();
            var response = await _httpClient.PostAsync($"{_apiBaseUrl}/api/Home/Order", null);
            if (response.IsSuccessStatusCode)
                return (true, string.Empty);
            return (false, response.StatusCode == System.Net.HttpStatusCode.Unauthorized ? "Не авторизован." : "Не удалось оформить заказ.");
        }

        public async Task<object> GetAboutUsAsync()
        {
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/Home/AboutUs");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<object>();
        }

        public async Task<string> GetPrivacyAsync()
        {
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/Home/Privacy");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }

    public class LoginResponse
    {
        public int IdUser { get; set; }
        public string Email { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public int RoleId { get; set; }
        public string Token { get; set; } = null!;
    }

    #region ==== DTO-классы для запросов/ответов ====

    // DTO для отображения товара в каталоге
    public class CatalogDto
    {
        public int IdProduct { get; set; }
        public string ProductName { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int? Weight { get; set; }
        public int? Stock { get; set; }
        public string? CategoryName { get; set; }
        public string? PathToImage { get; set; }
        public int ReviewCount { get; set; }
        public decimal AverageRating { get; set; }
        public bool IsInCart { get; set; }
    }

    // DTO для одиночного отзыва
    public class ReviewDto
    {
        public int IdReview { get; set; }
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public int Rating { get; set; }
        public string? ReviewText { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }

    // ViewModel-DTO для отзывов
    public class ProductReviewsViewModelDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public List<ReviewDto> Reviews { get; set; } = new();
    }

    // DTO для добавления отзыва
    public class AddReviewRequest
    {
        public int ProductId { get; set; }
        public int Rating { get; set; }
        public string ReviewText { get; set; } = null!;
    }

    // DTO для регистрации
    public class RegisterRequest
    {
        public string PhoneNumber { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string ConfirmPassword { get; set; } = null!;
    }

    // DTO для логина
    public class LoginRequest
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    // DTO для профиля пользователя
    public class UserProfileDto
    {
        public int IdUser { get; set; }
        public string? Email { get; set; }
        public string Phone { get; set; } = null!;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Address { get; set; }
        public DateOnly? RegistrationDate { get; set; }
        public int RoleId { get; set; }
    }

    // DTO для элемента корзины
    public class CartItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string PathToImage { get; set; } = null!;
        public int Stock { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total { get; set; }
    }

    // DTO для добавления в корзину
    public class AddToCartRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;
    }

    // DTO для обновления количества
    public class UpdateCartRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    #endregion
}
