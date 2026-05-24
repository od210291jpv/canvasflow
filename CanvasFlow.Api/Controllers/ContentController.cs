using CanvasFlow.Api.Models;
using CanvasFlow.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CanvasFlow.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Requires authentication for all content actions
    public class ContentController : ControllerBase
    {
        private readonly IContentService _contentService;

        public ContentController(IContentService contentService)
        {
            _contentService = contentService;
        }

        // GET: api/Content/feed?page=1&limit=20&tags=tag1,tag2
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

        // GET: api/Content/get/{contentId}
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
            // Get current user ID from JWT token
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new { error = "User ID missing or invalid." });
            }

            try
            {
                // 1. Перевіряємо, чи взагалі надійшов файл
                if (model.File == null || model.File.Length == 0)
                {
                    return BadRequest(new { error = "Please select a valid media file." });
                }

                // 2. Створюємо шлях до папки wwwroot/uploads
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // 3. Генеруємо унікальне ім'я для файлу (щоб не перезаписати існуючі)
                // Використовуємо оригінальне розширення файлу (.jpg, .png, .mp4 тощо)
                var fileExtension = Path.GetExtension(model.File.FileName);
                var uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
                var physicalFilePath = Path.Combine(uploadsFolder, uniqueFileName);

                // 4. Фізично зберігаємо файл на диск
                using (var stream = new FileStream(physicalFilePath, FileMode.Create))
                {
                    await model.File.CopyToAsync(stream);
                }

                // 5. Формуємо відносний URL, який буде збережено в базу і відправлено на фронтенд
                var generatedImageUrl = "/uploads/" + uniqueFileName;

                // 6. Передаємо вже згенерований URL у ваш сервіс замість model.ImageUrl
                var newContent = await _contentService.UploadContentAsync(
                    userId,
                    model.Title,
                    model.Description,
                    generatedImageUrl, // <-- Тепер тут лежить правильний шлях
                    model.Tags);

                return CreatedAtAction(nameof(GetContentById), new { contentId = newContent.Id }, newContent);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // POST: api/Content/like/{contentId}
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

        // PUT: api/Content/edit/{contentId}
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
                    model.ImageUrl, 
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

        // DELETE: api/Content/delete/{contentId}
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
        
        // GET: api/Content/me
        [HttpGet("me")]
        public async Task<IActionResult> GetMyContent()
        {
            // Placeholder for viewing user's own content
            return Ok(new { message = "Successfully retrieved your content list (Implementation pending)." });
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