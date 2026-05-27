using System;
using System.Collections.Generic;

namespace RestaurantSystem.Shared.Dtos
{
    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public ProfileDto? Profile { get; set; }
    }

    public class RestaurantDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? ImagePath { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? PhoneNumber { get; set; }
        public string? OpeningHours { get; set; }
        public string? Website { get; set; }
        public double AverageRating { get; set; }
        public bool IsFavorite { get; set; }
    }

    public class CreateRestaurantWithMenuDto
    {
        public RestaurantDto Restaurant { get; set; } = new();
        public List<MenuItemDto> MenuItems { get; set; } = new();
    }

    public class MenuItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? ImagePath { get; set; }
        public int RestaurantId { get; set; }
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }

    public class ProfileDto
    {
        public string Username { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? AvatarPath { get; set; }
        public string? Bio { get; set; }
        public DateTime RegistrationDate { get; set; }
    }

    public class UpdateProfileDto
    {
        public string? FullName { get; set; }
        public string? Bio { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public class ChangePasswordDto
    {
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class UserStatsDto
    {
        public int RestaurantCount { get; set; }
        public int FavoriteCount { get; set; }
    }

    public class RestaurantMapDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? PhoneNumber { get; set; }
        public string? OpeningHours { get; set; }
    }
}
