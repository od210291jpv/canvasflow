using Microsoft.Playwright;
using NUnit.Framework;
using CanvasFlow.Tests.E2E.Pages;
using System.Threading.Tasks;
using FluentAssertions;
using System.Net;

namespace CanvasFlow.Tests.E2E.Tests
{
    [TestFixture]
    public class AuthCycleTests
    {
        private IPlaywright _playwright;
        private IBrowser _browser;
        private IPage _page;
        private LoginPage _loginPage;
        private ProfilePage _profilePage;

        private const string BaseUrl = "https://localhost:5001"; // Adjust to actual running URL
        private const string TestUsername = "testuser";
        private const string TestPassword = "Password12CD!";

        [OneTimeSetUp]
        public async Task SetupAsync()
        {
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });
            _page = await _browser.NewPageAsync();
            _loginPage = new LoginPage(_page);
            _profilePage = new ProfilePage(_page);
        }

        [Test]
        public async Task AuthCycle_ShouldSucceed_And_Logout_ShouldInvalidateSession()
        {
            // 1. Login
            await _loginPage.NavigateAsync($"{BaseUrl}/Auth");
            await _loginPage.LoginAsync(TestUsername, TestPassword);

            // 2. Action: Verify we are on profile page and can see username
            // Note: This assumes the app redirects to Profile after login
            await _page.WaitForURLAsync($"{BaseUrl}/Profile");
            var username = await _profilePage.GetUsernameAsync();
            username.Should().NotBeNullOrEmpty();

            // 3. Logout
            await _profilePage.LogoutAsync();
            await _page.WaitForURLAsync($"{BaseUrl}/Auth");

            // 4. Verify 401: Try to access API directly via fetch in browser context
            var response = await _page.EvaluateAsync<IResponse>(@"async () => {
                const res = await fetch('https://localhost:5001/api/auth/me');
                return res;
            }");

            // In Playwright, we check the status code of the response
            var statusCode = await _page.EvaluateAsync<int>(@"async () => {
                const res = await fetch('https://localhost:5001/api/auth/me');
                return res.status;
            }");

            statusCode.Should().Be(401);
        }

        [OneTimeTearDown]
        public async Task TeardownAsync()
        {
            await _browser.CloseAsync();
            _playwright.Dispose();
        }
    }
}
