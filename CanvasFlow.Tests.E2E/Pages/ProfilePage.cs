using Microsoft.Playwright;
using System.Threading.Tasks;

namespace CanvasFlow.Tests.E2E.Pages
{
    public class ProfilePage
    {
        private readonly IPage _page;
        private readonly ILocator _usernameDisplay;
        private readonly ILocator _logoutButton;

        public ProfilePage(IPage page)
        {
            _page = page;
            _usernameDisplay = page.Locator("#user-name"); 
            _logoutButton = page.Locator("button#logout-btn"); 
        }

        public async Task<string?> GetUsernameAsync()
        {
            return await _usernameDisplay.InnerTextAsync();
        }

        public async Task LogoutAsync()
        {
            await _logoutButton.ClickAsync();
        }
    }
}
