using CanvasFlow.Api.Data;
using CanvasFlow.Api.DTO;
using CanvasFlow.Api.Models.Enums;
using CanvasFlow.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CanvasFlow.Api.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IContentService _contentService;
        private readonly ApplicationDbContext _context;

        public AdminController(IAuthService authService, IContentService contentService, ApplicationDbContext context)
        {
            _authService = authService;
            _contentService = contentService;
            _context = context;
        }

        [HttpPost("user/status")]
        public async Task<IActionResult> UpdateUserStatus([FromQuery] int targetUserId, [FromBody] UserStatus newStatus)
        {
            var adminUserId = GetCurrentAdminUserId();
            try
            {
                var updatedUser = await _authService.UpdateUserStatus(adminUserId, targetUserId, newStatus);
                return Ok(updatedUser);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpPost("user/block")]
        public async Task<IActionResult> BlockUser([FromQuery] int targetUserId)
        {
            var adminUserId = GetCurrentAdminUserId();
            try
            {
                var blockedUser = await _authService.BlockUser(adminUserId, targetUserId);
                return Ok(blockedUser);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpPost("user/message")]
        public async Task<IActionResult> SendCustomMessage([FromQuery] int recipientUserId, [FromBody] string content)
        {
            var adminUserId = GetCurrentAdminUserId();
            try
            {
                await _authService.SendAdminMessage(adminUserId, recipientUserId, content);
                return Ok("Custom message sent and logged successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("content/publish")]
        public async Task<IActionResult> ModerateContent([FromQuery] int contentId, [FromBody] bool isPublished)
        {
            var adminUserId = GetCurrentAdminUserId();
            try
            {
                var updatedContent = await _contentService.ModerateContentAsync(adminUserId, contentId, isPublished);
                return Ok(updatedContent);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("content/edit")]
        public async Task<IActionResult> EditContent([FromQuery] int contentId, [FromBody] ContentEditDto editDto)
        {
            var adminUserId = GetCurrentAdminUserId();
            try
            {
                var updatedContent = await _contentService.EditContentAsAdminAsync(
                    adminUserId,
                    contentId,
                    editDto.Title,
                    editDto.Description,
                    editDto.Tags
                );
                return Ok(updatedContent);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("content/delete")]
        public async Task<IActionResult> DeleteContent([FromQuery] int contentId)
        {
            var adminUserId = GetCurrentAdminUserId();
            try
            {
                await _contentService.DeleteContentAsync(adminUserId, contentId);
                return Ok("Content successfully soft-deleted and audited.");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("audit/logs")]
        public async Task<IActionResult> GetAuditLogs([FromQuery] int page = 1, [FromQuery] int limit = 20)
        {
            var adminUserId = GetCurrentAdminUserId();

            var logs = await _context.AuditLogs
                .Where(l => l.AdminUserId == adminUserId && !l.IsDeleted)
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            return Ok(logs);
        }

        private int GetCurrentAdminUserId()
        {
            if (User.Identity.IsAuthenticated && User.Claims.Any(c => c.Type == "sub"))
            {
                if (int.TryParse(User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value, out int userId))
                {
                    return userId;
                }
            }
            return 1;
        }
    }
}