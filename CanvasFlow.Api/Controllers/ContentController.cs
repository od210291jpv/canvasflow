using CanvasFlow.Api.DTO;
using CanvasFlow.Api.Hubs;
using CanvasFlow.Api.Services;
using CanvasFlow.Api.Services.CmsApi;
using CanvasFlow.Api.Services.CmsApi.Models;
using CanvasFlow.Db.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
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
        private readonly CmsApiClient _cmsApiClient;
        private const string CmsUsername = "CanvasFlow";
        private const string CmsPassword = "Password";

        public ContentController(
            IContentService contentService,
            IExternalStorageService externalStorageService,
            IHubContext<NotificationHub> notificationHub)
        {
            _contentService = contentService;
            _externalStorageService = externalStorageService;
            _notificationHub = notificationHub;
            _cmsApiClient = new CmsApiClient(new HttpClient(), "http://192.168.88.68:8085"); // Replace with actual CMS API base URL

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

            List<Content> feed;
            try
            {
                feed = await MapContent(page, limit, tagList);
            }
            catch (HttpRequestException ex) 
            {
                return Unauthorized($"Failed to fetch content feed: {ex.Message}");
            }

            return Ok(feed);
        }

        private async Task<List<Content>> MapContent(int page, int limit, List<string>? tagList)
        {
            CmsLoginResponseDto cmsUser;
            try
            {
                cmsUser = await _cmsApiClient.LoginAsync(CmsUsername, CmsPassword);

            }
            catch (HttpRequestException e)
            {
                throw new HttpRequestException($"Failed to login to CMS API. {e}");
            }

            List<Content> feed = await _contentService.GetFeedAsync(page, limit, tagList);
            ContentObjectDtoPagedResult cmsContent = await _cmsApiClient.GetUserContentAsync(cmsUser.User.Id, page, 10000);

            // Map stored imageUrl tokens "userId:cmsId" → cmsId key, userId value
            var ids = feed
                .Where(c => c.ImageUrl != null && c.ImageUrl.Contains(':'))
                .Select(c => c.ImageUrl!)
                .ToDictionary(
                    url => url.Split(':').Last(),   // key   = cmsId
                    url => url.Split(':').First());  // value = userId

            // Build lookup of active CMS items only (Enabled=true, not deleted)
            var activeCmsItems = cmsContent.Items
                .Where(c => c.Enabled && !c.IsDeleted)
                .ToDictionary(c => c.Id.ToString());

            // Resolve URLs for active items; collect CMS IDs that map to feed entries
            var resolvedCmsIds = new HashSet<string>();
            foreach (var cmsItem in activeCmsItems.Values)
            {
                if (ids.TryGetValue(cmsItem.Id.ToString(), out var userId) && userId is not null)
                {
                    var feedItem = feed.SingleOrDefault(i => i.ImageUrl == $"{userId}:{cmsItem.Id}");
                    if (feedItem is not null)
                    {
                        feedItem.ImageUrl = cmsItem.Path;
                        resolvedCmsIds.Add(cmsItem.Id.ToString());
                    }
                }
            }

            // Remove feed entries whose CMS content is disabled/deleted or simply not found
            feed.RemoveAll(item =>
            {
                var parts = item.ImageUrl?.Split(':');
                // If the imageUrl still looks like "userId:cmsId" it was never resolved → remove it
                if (parts is { Length: 2 } && int.TryParse(parts[1], out _))
                    return true;
                return false;
            });

            return feed;
        }

        [HttpGet("get/{contentId}")]
        public async Task<IActionResult> GetContentById(int contentId)
        {
            CmsLoginResponseDto cmsUser;
            try
            {
                cmsUser = await _cmsApiClient.LoginAsync(CmsUsername, CmsPassword);

            }
            catch (HttpRequestException e)
            {
                throw new HttpRequestException($"Failed to login to CMS API. {e}");
            }

            var content = await _contentService.GetContentByIdAsync(contentId);

            if (content == null)
            {
                return NotFound(new { error = "Content not found." });
            }

            ContentModel cmsContent = await _cmsApiClient.GetContentByContentIdAsync(int.Parse(content.ImageUrl.Split(":").Last()));

            if (cmsContent == null)
            {
                return NotFound(new { error = "Content not found in CMS" });
            }

            content.ImageUrl = cmsContent.Path;

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
                CmsLoginResponseDto cmsUser;
                try
                {
                    cmsUser = await _cmsApiClient.LoginAsync(CmsUsername, CmsPassword);

                }
                catch (HttpRequestException e) 
                {
                    return BadRequest(new { error = $"Failed to login to CMS API: {e.Message}" });
                }

                using var fileStream = model.File.OpenReadStream();
                var fileContent = new StreamContent(fileStream);

                ContentModel cmsContentSubmitted = await _cmsApiClient.CreateContentAsync(fileContent, model.File.FileName, cmsUser.User.Id, true, model.Description, true, false );
                
                if(cmsContentSubmitted is null)
                {
                    return BadRequest(new { error = "Failed to create content in CMS." });
                }

                var newContent = await _contentService.UploadContentAsync(
                    userId,
                    model.Title,
                    model.Description,
                    $"{userId}:{cmsContentSubmitted.Id}",
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

            CmsLoginResponseDto cmsUser;
            try
            {
                cmsUser = await _cmsApiClient.LoginAsync(CmsUsername, CmsPassword);

            }
            catch (HttpRequestException e)
            {
                throw new HttpRequestException($"Failed to login to CMS API. {e}");
            }

            var targetContent = await _contentService.GetContentByIdAsync(contentId);

            await _cmsApiClient.DeleteContentAsync(int.Parse($"{targetContent.ImageUrl}".Split(":").Last()));
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
            CmsLoginResponseDto cmsUser;
            try
            {
                cmsUser = await _cmsApiClient.LoginAsync(CmsUsername, CmsPassword);
            }
            catch (HttpRequestException e)
            {
                return BadRequest(new { error = $"Failed to login to CMS API: {e.Message}" });
            }

            var myContent = await _contentService.GetContentByUserIdAsync(userId);

            // Resolve real image URLs from CMS for each content item
            var cmsContent = await _cmsApiClient.GetUserContentAsync(cmsUser.User.Id, 1, 10000);

            // Build a lookup: cmsItemId -> cmsPath
            var cmsPathById = cmsContent.Items
                .ToDictionary(c => c.Id.ToString(), c => c.Path);

            foreach (var item in myContent)
            {
                var parts = item.ImageUrl?.Split(':');
                if (parts != null && parts.Length == 2)
                {
                    var cmsItemId = parts[1];
                    if (cmsPathById.TryGetValue(cmsItemId, out var resolvedPath))
                    {
                        item.ImageUrl = resolvedPath;
                    }
                }
            }

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