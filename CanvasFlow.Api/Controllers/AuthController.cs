using Microsoft.AspNetCore.Mvc;
using CanvasFlow.Api.Services;
using CanvasFlow.Api.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace CanvasFlow.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            try
            {
                var user = await _authService.RegisterUser(model.Username, model.Email, model.Password);
                return Ok(new { message = "User registered successfully. Please check your email to activate.", user = user });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            try
            {
                var token = await _authService.Login(model.Username, model.Password);
                return Ok(new { token = token });
            }
            catch (Exception ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    return Unauthorized(new { error = "User ID not found in token." });
                }

                if (int.TryParse(userIdClaim.Value, out int userId))
                {
                    var user = await _authService.GetUserById(userId);
                    if (user == null)
                    {
                        return NotFound(new { error = "User not found." });
                    }

                    return Ok(new
                    {
                        user.Id,
                        user.Username,
                        user.Email,
                        user.Role,
                        user.AccountStatus,
                        user.PublicationCount
                    });
                }

                return BadRequest(new { error = "Invalid User ID format in token." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var authHeader = Request.Headers["Authorization"].ToString();
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return Unauthorized(new { error = "Authorization header is missing or invalid." });
                }

                var token = authHeader.Substring("Bearer ".Length).Trim();
                await _authService.Logout(token);

                return Ok(new { message = "Successfully logged out." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    public class RegisterDto
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
