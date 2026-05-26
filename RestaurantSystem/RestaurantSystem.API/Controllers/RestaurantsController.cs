using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RestaurantSystem.API.Repositories;
using RestaurantSystem.Shared.Dtos;
using RestaurantSystem.Shared.Entities;

namespace RestaurantSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RestaurantsController : ControllerBase
    {
        private readonly IRepository<Restaurant> _restaurantRepo;
        private readonly IMemoryCache _cache;
        private const string CacheKey = "RestaurantsList";

        public RestaurantsController(IRepository<Restaurant> restaurantRepo, IMemoryCache cache)
        {
            _restaurantRepo = restaurantRepo;
            _cache = cache;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<RestaurantDto>>> GetAll(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null)
        {
            // Simple caching for the first page without search
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
                    ImagePath = r.ImagePath
                })
                .ToListAsync();

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

            return new RestaurantDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                Address = r.Address,
                ImagePath = r.ImagePath
            };
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<RestaurantDto>> Create(RestaurantDto dto)
        {
            var r = new Restaurant
            {
                Name = dto.Name,
                Description = dto.Description,
                Address = dto.Address,
                ImagePath = dto.ImagePath
            };

            await _restaurantRepo.AddAsync(r);
            await _restaurantRepo.SaveChangesAsync();
            _cache.Remove(CacheKey);

            dto.Id = r.Id;
            return CreatedAtAction(nameof(GetById), new { id = r.Id }, dto);
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, RestaurantDto dto)
        {
            var r = await _restaurantRepo.GetByIdAsync(id);
            if (r == null) return NotFound();

            r.Name = dto.Name;
            r.Description = dto.Description;
            r.Address = dto.Address;
            r.ImagePath = dto.ImagePath;

            _restaurantRepo.Update(r);
            await _restaurantRepo.SaveChangesAsync();
            _cache.Remove(CacheKey);

            return NoContent();
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var r = await _restaurantRepo.GetByIdAsync(id);
            if (r == null) return NotFound();

            _restaurantRepo.Remove(r);
            await _restaurantRepo.SaveChangesAsync();
            _cache.Remove(CacheKey);

            return NoContent();
        }
    }
}
