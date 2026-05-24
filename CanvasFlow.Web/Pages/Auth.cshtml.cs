using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using System.Text.Json;

namespace CanvasFlow.Web.Pages
{
    public class AuthModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AuthModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty]
        public LoginDto LoginData { get; set; } = new();

        [BindProperty]
        public RegisterDto RegisterData { get; set; } = new();

        public string? Message { get; set; }
        public bool IsError { get; set; }
        public bool IsLoginMode { get; set; } = true;

        public void OnGet(bool loginMode = true)
        {
            IsLoginMode = loginMode;
        }

        public async Task<IActionResult> OnPostLoginAsync()
        {
            IsLoginMode = true;
            var client = _httpClientFactory.CreateClient("ApiUrl");
            
            try
            {
                var response = await client.PostAsJsonAsync("api/auth/login", LoginData);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonDocument.Parse(content);
                    var token = result.RootElement.GetProperty("token").GetString();
                    
                    // Store token in session for the Profile page to use
                    HttpContext.Session.SetString("AuthToken", token ?? "");

                    Message = "Login Successful! Redirecting...";
                    IsError = false;

                    // Small delay to allow user to see success message before redirect
                    await Task.Delay(1000);
                    return RedirectToPage("/Profile");
                }
                else
                {
                    var errorObj = JsonDocument.Parse(content);
                    Message = errorObj.RootElement.GetProperty("error").GetString();
                    IsError = true;
                }
            }
            catch (Exception ex)
            {
                Message = $"Error: {ex.Message}";
                IsError = true;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostRegisterAsync()
        {
            IsLoginMode = false;
            var client = _httpClientFactory.CreateClient("ApiUrl");

            try
            {
                var response = await client.PostAsJsonAsync("api/auth/register", RegisterData);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    Message = "Registration successful! Please check your email.";
                    IsError = false;
                }
                else
                {
                    var errorObj = JsonDocument.Parse(content);
                    Message = errorObj.RootElement.GetProperty("error").GetString();
                    IsError = true;
                }
            }
            catch (Exception ex)
            {
                Message = $"Error: {ex.Message}";
                IsError = true;
            }

            return Page();
        }
    }

    public class LoginDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterDto
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
