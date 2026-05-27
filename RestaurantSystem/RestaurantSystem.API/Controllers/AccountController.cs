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
            if (request.Password != request.ConfirmPassword)
                return BadRequest("Пароли не совпадают");

            if (await _userRepo.GetQueryable().AnyAsync(u => u.Username == request.Username))
                return BadRequest("Имя пользователя уже занято");

            var user = new AppUser
            {
                Username = request.Username,
                FullName = request.FullName,
                PasswordHash = _authService.HashPassword(request.Password),
                RegistrationDate = DateTime.UtcNow
            };

            await _userRepo.AddAsync(user);
            await _userRepo.SaveChangesAsync();

            var profile = new ProfileDto
            {
                Username = user.Username,
                FullName = user.FullName,
                RegistrationDate = user.RegistrationDate
            };

            return new AuthResponse
            {
                Token = _authService.CreateToken(user),
                Username = user.Username,
                Profile = profile
            };
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
        {
            var user = await _userRepo.GetQueryable().FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null || !_authService.VerifyPassword(user.PasswordHash, request.Password))
                return Unauthorized("Неверное имя пользователя или пароль");

            var profile = new ProfileDto
            {
                Username = user.Username,
                FullName = user.FullName,
                AvatarPath = user.AvatarPath,
                Bio = user.Bio,
                RegistrationDate = user.RegistrationDate
            };

            return new AuthResponse
            {
                Token = _authService.CreateToken(user),
                Username = user.Username,
                Profile = profile
            };
        }
    }
}
