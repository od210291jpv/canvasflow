using CanvasFlow.Api.DTO;
using CanvasFlow.Api.Hubs;
using CanvasFlow.Api.Services;
using CanvasFlow.Db.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace CanvasFlow.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ContentController : ControllerBase
    {
        private readonly IContentService _contentService;
        private readonly IExternalStorageService _externalStorageService;
        private readonly IHubContext<NotificationHub> _notificationHub;

        public ContentController(
            IContentService contentService,
            IExternalStorageService externalStorageService,
            IHubContext<NotificationHub> notificationHub)
        {
            _contentService = contentService;
            _externalStorageService = externalStorageService;
            _notificationHub = notificationHub;
        }

        [AllowAnonymous]
        [HttpGet("feed")]
        public async Task<IActionResult> GetFeed(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20,
            [FromQuery] string? tags = null)
        {
            List<string>? tagList = null;
            if (!string.IsNullOrEmpty(tags))
            {
                tagList = tags.Split(',').ToList();
            }

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

            if (model.File == null)
            {
                return BadRequest(new { error = "File is missing." });
            }

            try
            {
                using var memoryStream = new MemoryStream();
                await model.File.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();

                var safeExt = Path.GetExtension(model.File.FileName).ToLowerInvariant();
                var safeFileName = $"img_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{safeExt}";

                await _externalStorageService.UploadImageAsync(fileBytes, safeFileName, model.File.ContentType);

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
                var (stream, contentType) = await _externalStorageService.GetImageStreamAsync(fileName);

                if (stream == null)
                {
                    return NotFound(new { error = "Image not found on ESP32." });
                }

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
                var content = await _externalStorageService.GetExternalImagesJsonAsync();
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

            if (content.UserId == userId)
            {
                return BadRequest(new { error = "You cannot like your own content." });
            }

            var success = await _contentService.LikeContentAsync(contentId, userId);

            if (success)
            {
                await _notificationHub.Clients.User(content.UserId.ToString()).SendAsync("ReceiveNotification", new
                {
                    Title = "New Like",
                    Content = $"Someone liked your content: {content.Title}"
                });

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

        [HttpGet("tags")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTags()
        {
            var tags = await _contentService.GetAllTagsAsync();
            return Ok(tags);
        }
    }
}