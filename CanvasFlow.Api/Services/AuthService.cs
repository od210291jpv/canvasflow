using CanvasFlow.Api.Data;
using CanvasFlow.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CanvasFlow.Api.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<User> RegisterUserAsync(string username, string email, string password)
        {
            if (await _context.Users.AnyAsync(u => u.Username == username))
            {
                throw new Exception("Username already taken.");
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            var newUser = new User
            {
                Username = username,
                Email = email,
                PasswordHash = passwordHash,
                Role = "User", // Default role
                AccountStatus = "Pending" // Default status
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return newUser;
        }

        public async Task<(string Token, User User)> LoginAsync(string username, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (user == null || user.AccountStatus != "Active")
            {
                throw new Exception("Invalid credentials or account is inactive.");
            }

            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                throw new Exception("Invalid credentials.");
            }

            var token = GenerateJwtToken(user);

            return (token, user);
        }

        private string GenerateJwtToken(User user)
        {
            var jwtIssuer = _configuration["Jwt:Issuer"] ?? "CanvasFlow";
            var jwtAudience = _configuration["Jwt:Audience"] ?? "CanvasFlowClients";
            var jwtSecret = _configuration["Jwt:Key"] ?? "ThisIsASuperSecretKeyForTesting123!";

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = jwtIssuer,
                Audience = jwtAudience,
                SigningCredentials = credentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}