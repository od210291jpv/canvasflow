using CanvasFlow.Api.Models;
using CanvasFlow.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Security.Claims;

namespace CanvasFlow.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ContentController : ControllerBase
    {
        private readonly IContentService _contentService;
        private readonly IHttpClientFactory _httpClientFactory;

        public ContentController(IContentService contentService, IHttpClientFactory httpClientFactory)
        {
            _contentService = contentService;
            _httpClientFactory = httpClientFactory;
        }

        [AllowAnonymous]
        [HttpGet("feed")]
        public async Task<IActionResult> GetFeed(
            [FromQuery] int page = 1, 
            [FromQuery] int limit = 20, 
            [FromQuery] string tags = null)
        {
            List<string>? tagList = null;
            if (!string.IsNullOrEmpty(tags))
            {
                tagList = tags.Split(',').ToList();
            }

            List<Content> feed = await _contentService.GetFeedAsync(page, limit);
            return Ok(feed);
        }

        [HttpGet("get/{contentId}")]
        public async Task<IActionResult> GetContentById(int contentId)
        {
            var content = await _contentService.GetContentByIdAsync(contentId);

            if (content == null)
            {
                return NotFound(new { error = "Content not found." });
            }
            return Ok(content);
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadContent([FromForm] UploadContentDto model)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new { error = "User ID missing or invalid." });
            }

            try
            {
                using var memoryStream = new MemoryStream();
                await model.File.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();

                // 2. Створюємо клієнт
                using var client = _httpClientFactory.CreateClient();
                // Вимикаємо Keep-Alive про всяк випадок
                client.DefaultRequestHeaders.ConnectionClose = true;

                // 3. Формуємо контент з масиву байтів (це гарантує наявність Content-Length)
                using var fileContent = new ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(model.File.ContentType);

                using var multipartFormContent = new MultipartFormDataContent();
                // Важливо: "image" має точно збігатися з name="image" у вашій HTML-формі
                multipartFormContent.Add(fileContent, "image", model.File.FileName);

                // 4. Відправляємо запит
                var espResponse = await client.PostAsync("http://192.168.88.98/api/upload", multipartFormContent);

                if (!espResponse.IsSuccessStatusCode)
                {
                    var espError = await espResponse.Content.ReadAsStringAsync();
                    return BadRequest(new { error = $"ESP32 Upload Failed: {espError}" });
                }

                // 5. Зберігаємо відносне посилання для фронтенду (як ми робили в попередніх кроках)
                var generatedImageUrl = $"/api/content/proxy-image/{model.File.FileName}";

                var newContent = await _contentService.UploadContentAsync(
                    userId,
                    model.Title,
                    model.Description,
                    generatedImageUrl,
                    model.Tags);

                return CreatedAtAction(nameof(GetContentById), new { contentId = newContent.Id }, newContent);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("external-images")]
        [AllowAnonymous]
        public async Task<IActionResult> GetExternalImages()
        {
            try
            {
                using var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync("http://192.168.88.98/api/get_images");

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, new { error = "Failed to fetch images from ESP32" });
                }

                // Зчитуємо JSON з ESP32 і віддаємо його клієнту як є
                var content = await response.Content.ReadAsStringAsync();
                return Content(content, "application/json");
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("like/{contentId}")]
        public async Task<IActionResult> LikeContent(int contentId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new Exception("User ID missing."));
            
            var success = await _contentService.LikeContentAsync(contentId, userId);
            
            if (success)
            {
                return Ok(new { message = "Content liked successfully." });
            }
            return NotFound(new { error = "Content not found or user cannot like this content." });
        }

        [HttpPut("edit/{contentId}")]
        public async Task<IActionResult> EditContent(int contentId, [FromBody] UpdateContentDto model)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new Exception("User ID missing."));

            try
            {
                var updatedContent = await _contentService.UpdateContentAsync(
                    contentId, 
                    model.Title, 
                    model.Description, 
                    model.Tags);
                return Ok(updatedContent);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("delete/{contentId}")]
        public async Task<IActionResult> DeleteContent(int contentId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new Exception("User ID missing."));
            
            var success = await _contentService.DeleteContentAsync(userId, contentId);
            
            if (success)
            {
                return NoContent();
            }
            return NotFound(new { error = "Content not found or unauthorized to delete." });
        }

        [HttpGet("me")]
        [AllowAnonymous]
        public async Task<IActionResult> GetMyContent()
        {
            // Отримуємо ID користувача
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new { error = "User ID missing or invalid." });
            }
            Console.WriteLine($"\n---> ШУКАЮ ПУБЛІКАЦІЇ ДЛЯ USER ID: {userId} <--- \n");
            try
            {
                var myContent = await _contentService.GetContentByUserIdAsync(userId);
                return Ok(myContent); // Має повертати List<Content>
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }

    // DTOs for clean request/response handling
    public class UploadContentDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public IFormFile? File { get; set; }

        public List<string> Tags { get; set; } = new List<string>();
    }

    public class UpdateContentDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new List<string>();
    }
}