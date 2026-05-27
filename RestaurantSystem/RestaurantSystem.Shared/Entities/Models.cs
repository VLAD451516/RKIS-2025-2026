using System;
using System.Collections.Generic;

namespace RestaurantSystem.Shared.Entities
{
    public class AppUser
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? AvatarPath { get; set; }
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
        public string? Bio { get; set; }
        public DateTime? LastLoginDate { get; set; }
    }

    public class Restaurant
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
        public int CreatedByUserId { get; set; }
        public List<MenuItem> MenuItems { get; set; } = new();
    }

    public class MenuItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? ImagePath { get; set; }
        public int RestaurantId { get; set; }
    }

    public class Favorite
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int RestaurantId { get; set; }
    }
}
