using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CanvasFlow.Web.Pages
{
    [AllowAnonymous]
    public class ProfileModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ProfileModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public UserProfileDto? UserProfile { get; set; }
        public bool IsError { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var token = HttpContext.Request.Cookies["AuthToken"];

            if (string.IsNullOrEmpty(token))
            {
                return RedirectToPage("/Auth");
            }

            var client = _httpClientFactory.CreateClient("ApiUrl");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            try{
                var response = await client.GetAsync("api/auth/me");

                if (response.IsSuccessStatusCode)
                {
                    UserProfile = await response.Content.ReadFromJsonAsync<UserProfileDto>();
                }
                else
                {
                    IsError = true;
                    ErrorMessage = "Failed to load profile. Please login again.";
                    // Clear session if unauthorized
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        Response.Cookies.Delete("AuthToken");
                    }
                }
            }
            catch (Exception ex)
            {
                IsError = true;
                ErrorMessage = ex.Message;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostLogoutAsync()
        {
            Response.Cookies.Delete("AuthToken");
            return RedirectToPage("/Auth");
        }
    }

    public class UserProfileDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string AccountStatus { get; set; } = string.Empty;
        public int PublicationCount { get; set; }
    }
}
