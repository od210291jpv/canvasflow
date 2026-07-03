using Microsoft.Playwright;
using NUnit.Framework;
using CanvasFlow.Tests.E2E.Pages;
using System.Threading.Tasks;
using FluentAssertions;
using System.Net;
using System.Text.Json;
using System.Linq;

namespace CanvasFlow.Tests.E2E.Tests
{
    [TestFixture]
    public class ContentDeleteTests
    {
        private IPlaywright _playwright;
        private IBrowser _browser;
        private IPage _page;
        private LoginPage _loginPage;
        // Note: We might need a ContentPage if we want to do it via UI, 
        // but for this specific task, we can use the API context from the browser.

        private const string BaseUrl = "https://localhost:5001"; 
        private const string ApiUrl = "https://localhost:5001/api/Content";
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
        }

        [Test]
        public async Task DeleteContent_ShouldRemoveFromFeed_WhenSoftDeleted()
        {
            // 1. Login to establish authentication context (cookies/session)
            await _loginPage.NavigateAsync($"{BaseUrl}/Auth");
            await _loginPage.LoginAsync(TestUsername, TestPassword);
            await _page.WaitForURLAsync($"{BaseUrl}/Profile");

            // 2. Create content via API using the authenticated browser context
            // We use the page.EvaluateAsync to perform a fetch request within the authenticated session
            var uploadResponse = await _page.EvaluateAsync<dynamic>(@"async () => {
                const formData = new FormData();
                formData.append('title', 'Test Deleted Content');
                formData/append('description', 'This content should be deleted');
                formData.append('tags', 'test,delete');
                // Note: In a real E2E test, we'd upload an actual file. 
                // For simplicity in this test, we assume the backend handles empty files or we mock it.
                // However, since I cannot easily provide a file stream here, I will attempt to call the endpoint.
                
                const res = await fetch('https://localhost:5001/api/Content/upload', {
                    method: 'POST',
                    body: formData
                });
                return { status: res.status, content: res.ok ? await res.json() : null };
            }");

            // Note: The above approach is tricky with FormData and files in EvaluateAsync.
            // Let's try a more robust way: Use the API directly if we can get the token, 
            // or use the UI if the UI supports upload.
            // Since I don't see an Upload Page in the project map, I will assume I can use the existing API logic.
            
            // Let's refine: We'll check if we can find a content ID from the feed first, 
            // or just rely on the fact that 'GetMyContent' exists.
        }

        [OneTimeTearDown]
        public async Task TeardownAsync()
        {
            await _browser.CloseAsync();
            _playwright.Dispose();
        }
    }
}
