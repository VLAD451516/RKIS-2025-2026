using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using RestaurantSystem.Shared.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RestaurantSystem.API.Services
{
    public interface IAuthService
    {
        string CreateToken(AppUser user);
        string HashPassword(string password);
        bool VerifyPassword(string hashedPassword, string providedPassword);
    }

    public class AuthService : IAuthService
    {
        private readonly IConfiguration _config;
        private readonly PasswordHasher<AppUser> _passwordHasher = new();

        public AuthService(IConfiguration config)
        {
            _config = config;
        }

        public string CreateToken(AppUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "SecretKeyForJwtAuthentication"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(7),
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

        public string HashPassword(string password) => _passwordHasher.HashPassword(new AppUser(), password);

        public bool VerifyPassword(string hashedPassword, string providedPassword)
        {
            var result = _passwordHasher.VerifyHashedPassword(new AppUser(), hashedPassword, providedPassword);
            return result == PasswordVerificationResult.Success;
        }
    }
}
