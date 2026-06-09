// Models/User.cs
using CanvasFlow.Api.Models.Enums;

namespace CanvasFlow.Api.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "User"; // User or Admin
        public UserStatus AccountStatus { get; set; } = UserStatus.Pending; // Pending, Active, Blocked
        public bool IsDeleted { get; set; } = false; // Soft Delete
        public int PublicationCount { get; set; } = 0;
    }
}