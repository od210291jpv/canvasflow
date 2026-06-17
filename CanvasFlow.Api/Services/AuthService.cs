using CanvasFlow.Api.Data;
using CanvasFlow.Api.Models;
using CanvasFlow.Api.Models.Enums;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CanvasFlow.Api.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly INotificationService _notificationService;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;

        public AuthService(
            ApplicationDbContext context,
            IAuditService auditService,
            INotificationService notificationService,
            IConfiguration configuration,
            IMemoryCache cache)
        {
            _context = context;
            _auditService = auditService;
            _notificationService = notificationService;
            _configuration = configuration;
            _cache = cache;
        }

        public async Task<string> Login(string username, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username && !u.IsDeleted);

            if (user == null || user.AccountStatus != UserStatus.Active)
            {
                throw new UnauthorizedAccessException("Invalid credentials or account is inactive.");
            }

            if (user.PasswordHash != HashPassword(password))
            {
                throw new UnauthorizedAccessException("Invalid credentials.");
            }

            return GenerateJwtToken(user);
        }

        public async Task<User?> GetUserById(int userId)
        {
            return await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
        }

        /// <summary>
        /// Invalidates a token (Logout) by adding it to the MemoryCache blacklist.
        /// </summary>
        public async Task Logout(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(token))
                {
                    throw new ArgumentException("Invalid token format.");
                }

                var jwtToken = handler.ReadJwtToken(token);
                var expiration = jwtToken.ValidTo;
                var remainingTime = expiration - DateTime.UtcNow;

                if (remainingTime > TimeSpan.Zero)
                {
                    // Add token to blacklist with remaining lifetime as TTL
                    var cacheKey = $"blacklist_{token}";
                    _cache.Set(cacheKey, true, remainingTime);

                    await _auditService.LogActionAsync(0, "Token Blacklisted", "Auth", 0, $"Logout successful. Token blacklisted until {expiration}");
                }
            }
            catch (Exception ex)
            {
                await _auditService.LogActionAsync(0, "Logout Error", "Auth", 0, $"Logout failed: {ex.Message}");
                throw new ArgumentException("Could not process logout due to invalid token.", ex);
            }

            await Task.CompletedTask;
        }

        private string GenerateJwtToken(User user)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? "ThisIsASuperSecretKeyForTesting123!";
            var jwtIssuer = _configuration["Jwt:Issuer"] ?? "CanvasFlow";
            var jwtAudience = _configuration["Jwt:Audience"] ?? "CanvasFlowClients";

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("sub", user.Id.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<User> RegisterUser(string username, string email, string password)
        {
            if (await _context.Users.AnyAsync(u => u.Username == username))
            {
                throw new InvalidOperationException("Username already taken.");
            }

            var newUser = new User
            {
                Username = username,
                Email = email,
                PasswordHash = HashPassword(password),
                Role = "User",
                AccountStatus = UserStatus.Pending
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            await _auditService.LogActionAsync(0, "User Registered", "User", newUser.Id, $"New user registered: {username}");

            return newUser;
        }

        public async Task<User> UpdateUserStatus(int adminUserId, int targetUserId, UserStatus newStatus)
        {
            var user = await _context.Users.FindAsync(targetUserId);
            if (user == null)
            {
                throw new KeyNotFoundException("User not found.");
            }

            var adminUser = await _context.Users.FindAsync(adminUserId);
            if (adminUser == null || adminUser.Role != "Admin")
            {
                throw new UnauthorizedAccessException("Only Admins can change user status.");
            }

            user.AccountStatus = newStatus;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            await _auditService.LogActionAsync(adminUserId, $"User Status Changed to {newStatus}", "User", targetUserId, $"Status changed from {user.AccountStatus} to {newStatus}");

            return user;
        }

        public async Task<User> BlockUser(int adminUserId, int targetUserId)
        {
            return await UpdateUserStatus(adminUserId, targetUserId, UserStatus.Blocked);
        }

        public async Task SendAdminMessage(int adminUserId, int recipientId, string content)
        {
            await _notificationService.SendNotificationAsync(
                recipientId: recipientId, // Note: I'll fix this typo in the actual file write if found
                senderId: adminUserId,
                title: "Admin Message",
                content: "You have received a private message from the platform administration.",
                triggerType: "Admin"
            );

            await _auditService.LogActionAsync(adminUserId, "Sent Custom Message", "User", recipientId, $"Admin sent message: {content}");
        }

        private string HashPassword(string password)
        {
            return $"HASHED_{password.ToUpper()}";
        }
    }
}