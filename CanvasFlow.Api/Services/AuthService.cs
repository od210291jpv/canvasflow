// Services/AuthService.cs
using CanvasFlow.Api.Data;
using CanvasFlow.Api.Models;
using CanvasFlow.Api.Models.Enums;
using CanvasFlow.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace CanvasFlow.Api.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly INotificationService _notificationService;
        private readonly IConfiguration _configuration;

        public AuthService(ApplicationDbContext context, IAuditService auditService, INotificationService notificationService, IConfiguration configuration)
        {
            _context = context;
            _auditService = auditService;
            _notificationService = notificationService;
            _configuration = configuration;
        }

        public async Task<string> Login(string username, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username && !u.IsDeleted);

            if (user == null || user.AccountStatus != UserStatus.Active)
            {
                throw new UnauthorizedAccessException("Invalid credentials or account is inactive.");
            }

            // Basic password check (In production, use proper hashing/comparison)
            if (user.PasswordHash != HashPassword(password))
            {
                throw new UnauthorizedAccessException("Invalid credentials.");
            }

            return GenerateJwtToken(user);
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
                new Claim("sub", user.Id.ToString()) // Add sub for compatibility
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
            // Check for existing user
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
                AccountStatus = UserStatus.Pending // New users start as Pending
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();
            
            // Audit the registration
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

            // Check if the performing user is an admin
            var adminUser = await _context.Users.FindAsync(adminUserId);
            if (adminUser == null || adminUser.Role != "Admin")
            {
                throw new UnauthorizedAccessException("Only Admins can change user status.");
            }

            // Logic to prevent status downgrade (e.g., cannot go from Active to Pending)
            // But ALLOW unblocking (Blocked to Active)
            if (user.AccountStatus == UserStatus.Active && newStatus == UserStatus.Pending)
            {
                throw new InvalidOperationException("Cannot revert an active user to pending.");
            }

            user.AccountStatus = newStatus;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // Audit the change
            await _auditService.LogActionAsync(adminUserId, $"User Status Changed to {newStatus}", "User", targetUserId, $"Status changed from {user.AccountStatus} to {newStatus}");

            return user;
        }

        public async Task<User> BlockUser(int adminUserId, int targetUserId)
        {
            return await UpdateUserStatus(adminUserId, targetUserId, UserStatus.Blocked);
        }

        public async Task SendAdminMessage(int adminUserId, int recipientId, string content)
        {
            // 1. Send the message (uses the existing messaging service)
            await _notificationService.SendNotificationAsync(
                recipientId: recipientId,
                senderId: adminUserId,
                title: "Admin Message",
                content: "You have received a private message from the platform administration.",
                triggerType: "Admin"
            );

            // 2. Audit the action
            await _auditService.LogActionAsync(adminUserId, "Sent Custom Message", "User", recipientId, $"Admin sent message: {content}");
        }

        // Helper method for hashing (Placeholder)
        private string HashPassword(string password)
        {
            // In a real app, use BCrypt or Argon2
            return $"HASHED_{password.ToUpper()}";
        }
    }
}