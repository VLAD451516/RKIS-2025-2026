using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using RestaurantSystem.Api.Models;
using RestaurantSystem.Api.Services;
using RestaurantSystem.Core.Entities;
using RestaurantSystem.Core.Interfaces;

namespace RestaurantSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RestaurantsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileService _fileService;
    private readonly IMemoryCache _cache;
    private const string RestaurantsCacheKey = "RestaurantsList";

    public RestaurantsController(IUnitOfWork unitOfWork, IFileService fileService, IMemoryCache cache)
    {
        _unitOfWork = unitOfWork;
        _fileService = fileService;
        _cache = cache;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Restaurant>>> GetAll([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (!_cache.TryGetValue(RestaurantsCacheKey, out IEnumerable<Restaurant>? restaurants))
        {
            restaurants = await _unitOfWork.Restaurants.GetAllAsync();
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(5));
            _cache.Set(RestaurantsCacheKey, restaurants, cacheOptions);
        }

        if (restaurants == null) return Ok(new List<Restaurant>());

        if (!string.IsNullOrEmpty(search))
        {
            restaurants = restaurants.Where(r => r.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                                              || r.Description.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var totalCount = restaurants.Count();
        var pagedRestaurants = restaurants
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        Response.Headers.Add("X-Total-Count", totalCount.ToString());

        return Ok(pagedRestaurants);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Restaurant>> GetById(int id)
    {
        var restaurant = await _unitOfWork.Restaurants.GetWithMenuItemsAsync(id);
        if (restaurant == null) return NotFound();
        return Ok(restaurant);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<Restaurant>> Create([FromForm] RestaurantDto dto, IFormFile? image)
    {
        var restaurant = new Restaurant
        {
            Name = dto.Name,
            Description = dto.Description,
            Address = dto.Address
        };

        if (image != null)
        {
            restaurant.ImagePath = await _fileService.SaveFileAsync(image, "restaurants");
        }

        await _unitOfWork.Restaurants.AddAsync(restaurant);
        await _unitOfWork.CompleteAsync();
        _cache.Remove(RestaurantsCacheKey);

        return CreatedAtAction(nameof(GetById), new { id = restaurant.Id }, restaurant);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, [FromForm] RestaurantDto dto, IFormFile? image)
    {
        var restaurant = await _unitOfWork.Restaurants.GetByIdAsync(id);
        if (restaurant == null) return NotFound();

        restaurant.Name = dto.Name;
        restaurant.Description = dto.Description;
        restaurant.Address = dto.Address;

        if (image != null)
        {
            if (!string.IsNullOrEmpty(restaurant.ImagePath))
                _fileService.DeleteFile(restaurant.ImagePath);

            restaurant.ImagePath = await _fileService.SaveFileAsync(image, "restaurants");
        }

        _unitOfWork.Restaurants.Update(restaurant);
        await _unitOfWork.CompleteAsync();
        _cache.Remove(RestaurantsCacheKey);

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var restaurant = await _unitOfWork.Restaurants.GetByIdAsync(id);
        if (restaurant == null) return NotFound();

        if (!string.IsNullOrEmpty(restaurant.ImagePath))
            _fileService.DeleteFile(restaurant.ImagePath);

        _unitOfWork.Restaurants.Remove(restaurant);
        await _unitOfWork.CompleteAsync();
        _cache.Remove(RestaurantsCacheKey);

        return NoContent();
    }
}
