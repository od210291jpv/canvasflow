// Services/IAuthService.cs
using CanvasFlow.Api.Models;
using CanvasFlow.Api.Models.Enums;

namespace CanvasFlow.Api.Services
{
    public interface IAuthService
    {
        /// <summary>
        /// Authenticates a user and returns a JWT token.
        /// </summary>
        Task<string> Login(string username, string password);

        /// <summary>
        /// Registers a new user account.
        /// </summary>
        Task<User> RegisterUser(string username, string email, string password);

        /// <summary>
        /// Gets the current user's profile based on their ID.
        /// </summary>
        Task<User?> GetUserById(int userId);

        /// <summary>
        /// Invalidates a token (Logout).
        /// </summary>
        Task Logout(string token);

        /// <summary>
        /// Admin action: Changes the status of a user account (e.g., Block, Activate).
        /// </summary>
        /// <param name="adminUserId">The ID of the admin performing the action.</param>
        /// <param name="targetUserId">The ID of the user being moderated.</param>
        /// <param name="newStatus">The new status (Active, Blocked, Pending).</param>
        /// <returns>The updated user profile.</returns>
        Task<User> UpdateUserStatus(int adminUserId, int targetUserId, UserStatus newStatus);

        /// <summary>
        /// Admin action: Blocks a user account.
        /// </summary>
        /// <param name="adminUserId">The ID of the admin performing the action.</param>
        /// <param name="targetUserId">The ID of the user being blocked.</param>
        /// <returns>The updated user profile.</returns>
        Task<User> BlockUser(int adminUserId, int targetUserId);

        /// <summary>
        /// Admin action: Sends a custom, system-generated message to a user.
        /// </summary>
        /// <param name="adminUserId">The ID of the admin sending the message.</param>
        /// <param name="recipientId">The ID of the recipient.</param>
        /// <param name="content">The message content.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SendAdminMessage(int adminUserId, int recipientId, string content);
    }
}
