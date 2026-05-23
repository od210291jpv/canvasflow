using Microsoft.AspNetCore.Mvc;
using CanvasFlow.Api.Services;
using CanvasFlow.Api.Models;

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
                var user = await _authService.RegisterUserAsync(model.Username, model.Email, model.Password);
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
                var (token, user) = await _authService.LoginAsync(model.Username, model.Password);
                return Ok(new { token = token, user = user });
            }
            catch (Exception ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
        }
    }

    // DTOs for clean request/response handling
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