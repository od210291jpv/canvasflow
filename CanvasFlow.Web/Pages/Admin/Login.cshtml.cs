using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using System.Text.Json;

namespace CanvasFlow.Web.Pages.Admin
{
    public class LoginModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private const string ApiBaseUrl = "http://192.168.88.68:5000";

        public LoginModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty]
        public AdminLoginDto LoginData { get; set; } = new();

        public string? Message { get; set; }
        public bool IsError { get; set; }

        public void OnGet()
        {
            // Clear any previous messages on page load
            Message = null;
            IsError = false;
        }

        public async Task<IActionResult> OnPostLoginAsync()
        {
            var client = _httpClientFactory.CreateClient("ApiUrl");

            try
            {
                var response = await client.PostAsJsonAsync($"{ApiBaseUrl}/api/auth/login", LoginData);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonDocument.Parse(content);
                    var token = result.RootElement.GetProperty("token").GetString();

                    // Store token in session for the Dashboard page to use
                    HttpContext.Session.SetString("AdminAuthToken", token ?? "");

                    Message = "Успішний вхід! Перенаправлення...";
                    IsError = false;

                    // Small delay to allow user to see success message before redirect
                    await Task.Delay(1000);
                    return RedirectToPage("/Admin/Dashboard");
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
                Message = $"Помилка підключення до сервера: {ex.Message}";
                IsError = true;
            }

            return Page();
        }
    }

    public class AdminLoginDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
