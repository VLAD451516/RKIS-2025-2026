using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RestaurantSystem.API.Repositories;
using RestaurantSystem.Shared.Dtos;
using RestaurantSystem.Shared.Entities;
using System.Security.Claims;

namespace RestaurantSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RestaurantsController : ControllerBase
    {
        private readonly IRepository<Restaurant> _restaurantRepo;
        private readonly IRepository<MenuItem> _menuRepo;
        private readonly IRepository<Favorite> _favoriteRepo;
        private readonly IMemoryCache _cache;
        private const string CacheKey = "RestaurantsList";

        public RestaurantsController(
            IRepository<Restaurant> restaurantRepo,
            IRepository<MenuItem> menuRepo,
            IRepository<Favorite> favoriteRepo,
            IMemoryCache cache)
        {
            _restaurantRepo = restaurantRepo;
            _menuRepo = menuRepo;
            _favoriteRepo = favoriteRepo;
            _cache = cache;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<RestaurantDto>>> GetAll(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null)
        {
            if (pageNumber == 1 && string.IsNullOrEmpty(searchTerm))
            {
                if (_cache.TryGetValue(CacheKey, out PagedResult<RestaurantDto>? cachedResult))
                {
                    return cachedResult!;
                }
            }

            var query = _restaurantRepo.GetQueryable();
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(r => r.Name.Contains(searchTerm) || r.Description.Contains(searchTerm));
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new RestaurantDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    Address = r.Address,
                    ImagePath = r.ImagePath,
                    AverageRating = r.AverageRating
                })
                .ToListAsync();

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                var favoriteIds = await _favoriteRepo.GetQueryable()
                    .Where(f => f.UserId == userId)
                    .Select(f => f.RestaurantId)
                    .ToListAsync();

                foreach (var item in items)
                {
                    item.IsFavorite = favoriteIds.Contains(item.Id);
                }
            }

            var result = new PagedResult<RestaurantDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            if (pageNumber == 1 && string.IsNullOrEmpty(searchTerm))
            {
                _cache.Set(CacheKey, result, TimeSpan.FromMinutes(5));
            }

            return result;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<RestaurantDto>> GetById(int id)
        {
            var r = await _restaurantRepo.GetByIdAsync(id);
            if (r == null) return NotFound();

            var dto = new RestaurantDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                Address = r.Address,
                ImagePath = r.ImagePath,
                Latitude = r.Latitude,
                Longitude = r.Longitude,
                PhoneNumber = r.PhoneNumber,
                OpeningHours = r.OpeningHours,
                Website = r.Website,
                AverageRating = r.AverageRating
            };

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                dto.IsFavorite = await _favoriteRepo.GetQueryable()
                    .AnyAsync(f => f.UserId == userId && f.RestaurantId == id);
            }

            return dto;
        }

        [Authorize]
        [HttpPost("with-menu")]
        public async Task<ActionResult<RestaurantDto>> CreateWithMenu(CreateRestaurantWithMenuDto dto)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var r = new Restaurant
            {
                Name = dto.Restaurant.Name,
                Description = dto.Restaurant.Description,
                Address = dto.Restaurant.Address,
                ImagePath = dto.Restaurant.ImagePath,
                Latitude = dto.Restaurant.Latitude,
                Longitude = dto.Restaurant.Longitude,
                PhoneNumber = dto.Restaurant.PhoneNumber,
                OpeningHours = dto.Restaurant.OpeningHours,
                Website = dto.Restaurant.Website,
                CreatedByUserId = userId
            };

            await _restaurantRepo.AddAsync(r);
            await _restaurantRepo.SaveChangesAsync();

            foreach (var itemDto in dto.MenuItems)
            {
                var item = new MenuItem
                {
                    Name = itemDto.Name,
                    Description = itemDto.Description,
                    Price = itemDto.Price,
                    ImagePath = itemDto.ImagePath,
                    RestaurantId = r.Id
                };
                await _menuRepo.AddAsync(item);
            }
            await _menuRepo.SaveChangesAsync();

            _cache.Remove(CacheKey);

            return CreatedAtAction(nameof(GetById), new { id = r.Id }, new RestaurantDto { Id = r.Id, Name = r.Name });
        }

        [HttpGet("map")]
        public async Task<ActionResult<IEnumerable<RestaurantMapDto>>> GetMap()
        {
            return await _restaurantRepo.GetQueryable()
                .Select(r => new RestaurantMapDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Address = r.Address,
                    Latitude = r.Latitude,
                    Longitude = r.Longitude,
                    PhoneNumber = r.PhoneNumber,
                    OpeningHours = r.OpeningHours
                })
                .ToListAsync();
        }

        [Authorize]
        [HttpGet("my")]
        public async Task<ActionResult<IEnumerable<RestaurantDto>>> GetMy()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            return await _restaurantRepo.GetQueryable()
                .Where(r => r.CreatedByUserId == userId)
                .Select(r => new RestaurantDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    Address = r.Address,
                    ImagePath = r.ImagePath
                })
                .ToListAsync();
        }
    }
}
