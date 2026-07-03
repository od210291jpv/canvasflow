using Microsoft.Playwright;
using System.Threading.Tasks;

namespace CanvasFlow.Tests.E2E.Pages
{
    public class LoginPage
    {
        private readonly IPage _page;
        private readonly ILocator _usernameInput;
        private readonly ILocator _passwordInput;
        private readonly ILocator _loginButton;

        public LoginPage(IPage page)
        {
            _page = page;
            _usernameInput = page.Locator("input[name='Username']");
            _passwordInput = page.Locator("input[name='Password']");
            _loginButton = page.Locator("button[type='submit']");
        }

        public async Task NavigateAsync(string url)
        {
            await _page.GotoAsync(url);
        }

        public async Task LoginAsync(string username, string password)
        {
            await _usernameInput.FillAsync(username);
            await _passwordInput.FillAsync(password);
            await _loginButton.ClickAsync();
        }
    }
}
