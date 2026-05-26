using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantSystem.API.Repositories;
using RestaurantSystem.API.Services;
using RestaurantSystem.Shared.Dtos;
using RestaurantSystem.Shared.Entities;
using System.Security.Claims;

namespace RestaurantSystem.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileController : ControllerBase
    {
        private readonly IRepository<AppUser> _userRepo;
        private readonly IRepository<Restaurant> _restaurantRepo;
        private readonly IRepository<Favorite> _favoriteRepo;
        private readonly IAuthService _authService;

        public ProfileController(
            IRepository<AppUser> userRepo,
            IRepository<Restaurant> restaurantRepo,
            IRepository<Favorite> favoriteRepo,
            IAuthService authService)
        {
            _userRepo = userRepo;
            _restaurantRepo = restaurantRepo;
            _favoriteRepo = favoriteRepo;
            _authService = authService;
        }

        [HttpGet]
        public async Task<ActionResult<ProfileDto>> GetProfile()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null) return NotFound();

            return new ProfileDto
            {
                Username = user.Username,
                FullName = user.FullName,
                AvatarPath = user.AvatarPath,
                Bio = user.Bio,
                RegistrationDate = user.RegistrationDate
            };
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile(UpdateProfileDto dto)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null) return NotFound();

            user.FullName = dto.FullName;
            user.Bio = dto.Bio;

            _userRepo.Update(user);
            await _userRepo.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null) return NotFound();

            if (!_authService.VerifyPassword(user.PasswordHash, dto.OldPassword))
                return BadRequest("Неверный старый пароль");

            user.PasswordHash = _authService.HashPassword(dto.NewPassword);
            _userRepo.Update(user);
            await _userRepo.SaveChangesAsync();

            return Ok();
        }

        [HttpGet("stats")]
        public async Task<ActionResult<UserStatsDto>> GetStats()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var restaurantCount = await _restaurantRepo.GetQueryable().CountAsync(r => r.CreatedByUserId == userId);
            var favoriteCount = await _favoriteRepo.GetQueryable().CountAsync(f => f.UserId == userId);

            return new UserStatsDto
            {
                RestaurantCount = restaurantCount,
                FavoriteCount = favoriteCount
            };
        }
    }
}
