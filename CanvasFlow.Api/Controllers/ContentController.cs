using CanvasFlow.Api.Models;
using CanvasFlow.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CanvasFlow.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ContentController : ControllerBase
    {
        private readonly IContentService _contentService;

        public ContentController(IContentService contentService)
        {
            _contentService = contentService;
        }

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
            // Get current user ID from JWT token
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new { error = "User ID missing or invalid." });
            }

            try
            {
                if (model.File == null || model.File.Length == 0)
                {
                    return BadRequest(new { error = "Please select a valid media file." });
                }

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileExtension = Path.GetExtension(model.File.FileName);
                var uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
                var physicalFilePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(physicalFilePath, FileMode.Create))
                {
                    await model.File.CopyToAsync(stream);
                }

                var generatedImageUrl = "/uploads/" + uniqueFileName;

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