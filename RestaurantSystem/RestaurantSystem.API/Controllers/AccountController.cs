using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantSystem.API.Repositories;
using RestaurantSystem.API.Services;
using RestaurantSystem.Shared.Dtos;
using RestaurantSystem.Shared.Entities;

namespace RestaurantSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IRepository<AppUser> _userRepo;
        private readonly IAuthService _authService;

        public AccountController(IRepository<AppUser> userRepo, IAuthService authService)
        {
            _userRepo = userRepo;
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
        {
            if (await _userRepo.GetQueryable().AnyAsync(u => u.Username == request.Username))
                return BadRequest("Username already exists");

            var user = new AppUser
            {
                Username = request.Username,
                PasswordHash = _authService.HashPassword(request.Password)
            };

            await _userRepo.AddAsync(user);
            await _userRepo.SaveChangesAsync();

            return new AuthResponse
            {
                Token = _authService.CreateToken(user),
                Username = user.Username
            };
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
        {
            var user = await _userRepo.GetQueryable().FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null || !_authService.VerifyPassword(user.PasswordHash, request.Password))
                return Unauthorized("Invalid username or password");

            return new AuthResponse
            {
                Token = _authService.CreateToken(user),
                Username = user.Username
            };
        }
    }
}
