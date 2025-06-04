namespace WebApplication11.Models.Helpers
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;

        public ApiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiBaseUrl = configuration["ApiBaseUrl"] ?? "https://localhost:7133"; // Базовый URL API
        }

        // Получение списка товаров
        public async Task<List<CatalogDto>> GetCatalogsAsync()
        {
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/Home");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<CatalogDto>>() ?? new List<CatalogDto>();
        }

        // Получение отфильтрованного каталога
        public async Task<List<CatalogDto>> GetCatalogAsync(string? filter, string? search, string? sort)
        {
            var query = $"?filter={filter}&search={search}&sort={sort}";
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/Home/Catalog{query}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<CatalogDto>>() ?? new List<CatalogDto>();
        }

        // Получение отзывов для товара
        public async Task<ProductReviewsViewModelDto> GetReviewsAsync(int id)
        {
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/Home/Reviews/{id}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ProductReviewsViewModelDto>() ?? new ProductReviewsViewModelDto();
        }

        // Добавление отзыва
        public async Task<bool> AddReviewAsync(AddReviewRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync($"{_apiBaseUrl}/api/Home/AddReview", request);
            return response.IsSuccessStatusCode;
        }

        // Регистрация пользователя
        public async Task<bool> RegisterAsync(RegisterRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync($"{_apiBaseUrl}/api/Home/Register", request);
            return response.IsSuccessStatusCode;
        }

        // Вход пользователя
        public async Task<UserProfileDto?> LoginAsync(LoginRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync($"{_apiBaseUrl}/api/Home/Login", request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<UserProfileDto>();
            }
            return null;
        }

        // Получение профиля пользователя
        public async Task<UserProfileDto?> GetProfileAsync()
        {
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/Home/Profile");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<UserProfileDto>();
            }
            return null;
        }

        // Выход пользователя
        public async Task<bool> LogoutAsync()
        {
            var response = await _httpClient.PostAsync($"{_apiBaseUrl}/api/Home/Logout", null);
            return response.IsSuccessStatusCode;
        }

        // Доступ к EasyData (для администраторов)
        public async Task<string> GetEasyDataAsync()
        {
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/Home/EasyData");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            return result;
        }

        // Получение корзины
        public async Task<IEnumerable<CartItemDto>?> GetCartAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/Home/Cart");
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return new List<CartItemDto>(); // Return empty list for unauthorized users
                }
                response.EnsureSuccessStatusCode();
                var cartItems = await response.Content.ReadFromJsonAsync<IEnumerable<CartItemDto>>();
                return cartItems;
            }
            catch (HttpRequestException ex)
            {
                // Log the error if needed
                Console.WriteLine($"Error fetching cart: {ex.Message}");
                return null; // Return null on other errors
            }
        }

        // Добавление товара в корзину
        public async Task<bool> AddToCartAsync(AddToCartRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync($"{_apiBaseUrl}/api/Home/AddToCart", request);
            return response.IsSuccessStatusCode;
        }

        // Обновление количества в корзине
        public async Task<bool> UpdateCartQuantityAsync(UpdateCartRequest request)
        {
            var response = await _httpClient.PutAsJsonAsync($"{_apiBaseUrl}/api/Home/UpdateCartQuantity", request);
            return response.IsSuccessStatusCode;
        }

        // Удаление товара из корзины
        public async Task<bool> RemoveCartItemAsync(int productId)
        {
            var response = await _httpClient.DeleteAsync($"{_apiBaseUrl}/api/Home/RemoveCartItem/{productId}");
            return response.IsSuccessStatusCode;
        }

        // Оформление заказа
        public async Task<bool> PlaceOrderAsync()
        {
            var response = await _httpClient.PostAsync($"{_apiBaseUrl}/api/Home/Order", null);
            return response.IsSuccessStatusCode;
        }

        // Информация о компании
        public async Task<object> GetAboutUsAsync()
        {
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/Home/AboutUs");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<object>();
        }

        // Политика конфиденциальности
        public async Task<string> GetPrivacyAsync()
        {
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/Home/Privacy");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
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

        // Вычисляемые поля
        public int ReviewCount { get; set; }      // общее количество рейтингованных отзывов
        public decimal AverageRating { get; set; } // средний рейтинг (0, если нет отзывов)
        public bool IsInCart { get; set; }        // заполняется отдельно, если пользователь залогинен
    }

    // DTO для одиночного отзыва с именем/фамилией пользователя
    public class ReviewDto
    {
        public int IdReview { get; set; }
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public int Rating { get; set; }         // r.Rating может быть null, но в DTO кладём 0 по умолчанию
        public string? ReviewText { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? FirstName { get; set; }  // подтягивается через r.User.FirstName
        public string? LastName { get; set; }   // через r.User.LastName
    }

    // ViewModel-DTO для ответа GET /Reviews/{id}
    public class ProductReviewsViewModelDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public List<ReviewDto> Reviews { get; set; } = new();
    }

    // DTO для запроса добавления нового отзыва
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

    // DTO для профиля пользователя (ответ на GET /Profile)
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

    // DTO для одного элемента корзины (ответ GET /Cart)
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

    // DTO для запроса “добавить в корзину”
    public class AddToCartRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;
    }

    // DTO для запроса “обновить количество”
    public class UpdateCartRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    #endregion
}
