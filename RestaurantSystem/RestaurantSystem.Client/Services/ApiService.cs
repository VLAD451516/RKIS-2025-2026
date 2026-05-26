using System.Net.Http.Json;
using RestaurantSystem.Shared.Dtos;
using Microsoft.AspNetCore.Components.Authorization;

namespace RestaurantSystem.Client.Services
{
    public class ApiService
    {
        private readonly HttpClient _http;
        private readonly AuthenticationStateProvider _authStateProvider;

        public ApiService(HttpClient http, AuthenticationStateProvider authStateProvider)
        {
            _http = http;
            _authStateProvider = authStateProvider;
        }

        private async Task HandleResponse(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(error, null, response.StatusCode);
            }
        }

        public async Task<PagedResult<RestaurantDto>?> GetRestaurantsAsync(int page = 1, string? search = null)
        {
            return await _http.GetFromJsonAsync<PagedResult<RestaurantDto>>($"api/restaurants?pageNumber={page}&searchTerm={search}");
        }

        public async Task<RestaurantDto?> GetRestaurantByIdAsync(int id)
        {
            return await _http.GetFromJsonAsync<RestaurantDto>($"api/restaurants/{id}");
        }

        public async Task<List<MenuItemDto>?> GetMenuItemsAsync(int restaurantId)
        {
            return await _http.GetFromJsonAsync<List<MenuItemDto>>($"api/menuitems/restaurant/{restaurantId}");
        }

        public async Task<AuthResponse?> LoginAsync(LoginRequest request)
        {
            var response = await _http.PostAsJsonAsync("api/account/login", request);
            await HandleResponse(response);
            return await response.Content.ReadFromJsonAsync<AuthResponse>();
        }

        public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
        {
            var response = await _http.PostAsJsonAsync("api/account/register", request);
            await HandleResponse(response);
            return await response.Content.ReadFromJsonAsync<AuthResponse>();
        }

        public async Task<string?> UploadFileAsync(MultipartFormDataContent content)
        {
            var response = await _http.PostAsync("api/files/upload", content);
            await HandleResponse(response);
            return await response.Content.ReadAsStringAsync();
        }
    }
}
