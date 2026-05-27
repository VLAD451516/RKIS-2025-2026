using System.Net.Http.Json;
using RestaurantSystem.Shared.Dtos;
using Microsoft.AspNetCore.Components.Authorization;

namespace RestaurantSystem.Client.Services
{
    public class ApiService
    {
        private readonly HttpClient _http;

        public ApiService(HttpClient http)
        {
            _http = http;
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

        public async Task CreateRestaurantWithMenuAsync(CreateRestaurantWithMenuDto dto)
        {
            var response = await _http.PostAsJsonAsync("api/restaurants/with-menu", dto);
            await HandleResponse(response);
        }

        public async Task<List<RestaurantDto>?> GetUserFavoritesAsync()
        {
            return await _http.GetFromJsonAsync<List<RestaurantDto>>("api/favorites");
        }

        public async Task AddToFavoritesAsync(int restaurantId)
        {
            var response = await _http.PostAsync($"api/favorites/{restaurantId}", null);
            await HandleResponse(response);
        }

        public async Task RemoveFromFavoritesAsync(int restaurantId)
        {
            var response = await _http.DeleteAsync($"api/favorites/{restaurantId}");
            await HandleResponse(response);
        }

        public async Task<ProfileDto?> GetProfileAsync()
        {
            return await _http.GetFromJsonAsync<ProfileDto>("api/profile");
        }

        public async Task UpdateProfileAsync(UpdateProfileDto dto)
        {
            var response = await _http.PutAsJsonAsync("api/profile", dto);
            await HandleResponse(response);
        }

        public async Task ChangePasswordAsync(ChangePasswordDto dto)
        {
            var response = await _http.PostAsJsonAsync("api/profile/change-password", dto);
            await HandleResponse(response);
        }

        public async Task<UserStatsDto?> GetUserStatsAsync()
        {
            return await _http.GetFromJsonAsync<UserStatsDto>("api/profile/stats");
        }

        public async Task<List<RestaurantDto>?> GetMyRestaurantsAsync()
        {
            return await _http.GetFromJsonAsync<List<RestaurantDto>>("api/restaurants/my");
        }

        public async Task<List<RestaurantMapDto>?> GetRestaurantsForMapAsync()
        {
            return await _http.GetFromJsonAsync<List<RestaurantMapDto>>("api/restaurants/map");
        }
    }
}
