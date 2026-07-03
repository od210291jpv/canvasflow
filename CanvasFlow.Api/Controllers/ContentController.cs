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

            // Pass the parsed tag list to the service
            List<Content> feed = await _contentService.GetFeedAsync(page, limit, tagList);
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

                var safeExt = Path.GetExtension(model.File.FileName).ToLowerInvariant();
                var safeFileName = $"img_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{safeExt}";

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
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(model.File.ContentType);
                fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                {
                    Name = "\"image\"",          
                    FileName = $"\"{safeFileName}\"" 
                };

                multipartFormContent.Add(fileContent);
                await multipartFormContent.LoadIntoBufferAsync();

                var espResponse = await client.PostAsync("http://192.168.88.98/api/upload", multipartFormContent);

                if (!espResponse.IsSuccessStatusCode)
                {
                    var espError = await espResponse.Content.ReadAsStringAsync();
                    return BadRequest(new { error = $"ESP32 Upload Failed: {espError}" });
                }

                var generatedImageUrl = $"/api/content/proxy-image/{safeFileName}";

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

        [AllowAnonymous]
        [HttpGet("proxy-image/{*fileName}")]
        public async Task<IActionResult> GetProxyImage(string fileName)
        {
            try
            {
                using var client = _httpClientFactory.CreateClient();

                var espUrl = $"http://192.168.88.98/images/{fileName}";
                var response = await client.GetAsync(espUrl);

                if (!response.IsSuccessStatusCode)
                {
                    return NotFound(new { error = "Image not found on ESP32." });
                }

                var stream = await response.Content.ReadAsStreamAsync();

                var contentType = fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                    ? "image/png"
                    : "image/jpeg";

                return File(stream, contentType);
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
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new { error = "User ID missing or invalid." });
            }

            var content = await _contentService.GetContentByIdAsync(contentId);
            if (content == null)
            {
                return NotFound(new { error = "Content not found." });
            }

            // Rule: UserId != AuthorId
            if (content.UserId == userId)
            {
                return BadRequest(new { error = "You cannot like your own content." });
            }

            var success = await _contentService.LikeContentAsync(contentId, userId);
            
            if (success)
            {
                return Ok(new { message = "Content liked successfully." });
            }
            return BadRequest(new { error = "Failed to like content." });
        }

        [HttpPut("edit/{contentId}")]
        public async Task<IActionResult> EditContent(int contentId, [FromBody] UpdateContentDto model)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new { error = "User ID missing or invalid." });
            }

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
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new { error = "User ID missing or invalid." });
            }
            
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
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new { error = "User ID missing or invalid." });
            }
            try
            {
                var myContent = await _contentService.GetContentByUserIdAsync(userId);
                return Ok(myContent);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }

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