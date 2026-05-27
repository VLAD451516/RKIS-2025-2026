using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantSystem.API.Repositories;
using RestaurantSystem.Shared.Dtos;
using RestaurantSystem.Shared.Entities;
using System.Security.Claims;

namespace RestaurantSystem.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class FavoritesController : ControllerBase
    {
        private readonly IRepository<Favorite> _favoriteRepo;
        private readonly IRepository<Restaurant> _restaurantRepo;

        public FavoritesController(IRepository<Favorite> favoriteRepo, IRepository<Restaurant> restaurantRepo)
        {
            _favoriteRepo = favoriteRepo;
            _restaurantRepo = restaurantRepo;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<RestaurantDto>>> GetUserFavorites()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var favoriteRestaurantIds = await _favoriteRepo.GetQueryable()
                .Where(f => f.UserId == userId)
                .Select(f => f.RestaurantId)
                .ToListAsync();

            return await _restaurantRepo.GetQueryable()
                .Where(r => favoriteRestaurantIds.Contains(r.Id))
                .Select(r => new RestaurantDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    Address = r.Address,
                    ImagePath = r.ImagePath,
                    IsFavorite = true
                })
                .ToListAsync();
        }

        [HttpPost("{restaurantId}")]
        public async Task<IActionResult> AddToFavorites(int restaurantId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            if (await _favoriteRepo.GetQueryable().AnyAsync(f => f.UserId == userId && f.RestaurantId == restaurantId))
                return BadRequest("Уже в избранном");

            await _favoriteRepo.AddAsync(new Favorite { UserId = userId, RestaurantId = restaurantId });
            await _favoriteRepo.SaveChangesAsync();

            return Ok();
        }

        [HttpDelete("{restaurantId}")]
        public async Task<IActionResult> RemoveFromFavorites(int restaurantId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var favorite = await _favoriteRepo.GetQueryable()
                .FirstOrDefaultAsync(f => f.UserId == userId && f.RestaurantId == restaurantId);

            if (favorite == null) return NotFound();

            _favoriteRepo.Remove(favorite);
            await _favoriteRepo.SaveChangesAsync();

            return Ok();
        }
    }
}
