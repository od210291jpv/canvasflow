using CanvasFlow.Api.Models;

namespace CanvasFlow.Api.Services
{
    public interface IAuthService
    {
        /// <summary>
        /// Registers a new user and returns the created user object.
        /// </summary>
        Task<User> RegisterUserAsync(string username, string email, string password);

        /// <summary>
        /// Authenticates a user and returns a JWT token.
        /// </summary>
        Task<(string Token, User User)> LoginAsync(string username, string password);
    }
}