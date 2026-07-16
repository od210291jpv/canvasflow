using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CanvasFlow.Api.Services
{
    public class ExternalStorageService : IExternalStorageService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private const string BaseUrl = "http://192.168.88.98";

        public ExternalStorageService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task UploadImageAsync(byte[] fileBytes, string fileName, string contentType)
        {
            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.ConnectionClose = true;
            client.DefaultRequestHeaders.ExpectContinue = false;

            using var multipartFormContent = new MultipartFormDataContent();
            var boundary = multipartFormContent.Headers.ContentType?.Parameters.FirstOrDefault(p => p.Name == "boundary");
            if (boundary != null && boundary.Value != null)
            {
                boundary.Value = boundary.Value.Replace("\"", "");
            }

            using var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = "\"image\"",
                FileName = $"\"{fileName}\""
            };

            multipartFormContent.Add(fileContent);
            await multipartFormContent.LoadIntoBufferAsync();

            var response = await client.PostAsync($"{BaseUrl}/api/upload", multipartFormContent);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"ESP32 Upload Failed: {error}");
            }
        }

        public async Task<(Stream? Stream, string ContentType)> GetImageStreamAsync(string fileName)
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{BaseUrl}/images/{fileName}");

            if (!response.IsSuccessStatusCode)
            {
                return (null, string.Empty);
            }

            // Копіюємо у MemoryStream, щоб уникнути проблеми із закриттям з'єднання HttpClient
            var memoryStream = new MemoryStream();
            await response.Content.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            var contentType = fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                ? "image/png"
                : "image/jpeg";

            return (memoryStream, contentType);
        }

        public async Task<string> GetExternalImagesJsonAsync()
        {
            using var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{BaseUrl}/api/get_images");

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Failed to fetch images from ESP32");
            }

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> CmsloginAsync(string login, string password)
        {
            using var client = _httpClientFactory.CreateClient();

            var loginData = new
            {
                login = login,
                password = password
            };

            var json = JsonSerializer.Serialize(loginData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{BaseUrl}/api/cmslogin", content);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Failed to login to CMS");
            }

            return await response.Content.ReadAsStringAsync();
        }
    }
}
