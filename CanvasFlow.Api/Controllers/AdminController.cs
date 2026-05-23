// Controllers/AdminController.cs
using CanvasFlow.Api.Data;
using CanvasFlow.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CanvasFlow.Api.Controllers
{
    [Authorize(Roles = "Admin")] // Only Admins can access this controller
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IContentService _contentService;
        private readonly IAuditService _auditService;
        private readonly ApplicationDbContext _context;

        public AdminController(IAuthService authService, IContentService contentService, IAuditService auditService, ApplicationDbContext context)
        {
            _authService = authService;
            _contentService = contentService;
            _auditService = auditService;
            _context = context;
        }

        // --- USER MODERATION ---

        /// <summary>
        /// Admin action: Approves or deactivates a user account.
        /// </summary>
        [HttpPost("user/status")]
        public async Task<IActionResult> UpdateUserStatus([FromQuery] int targetUserId, [FromBody] string newStatus)
        {
            var adminUserId = GetCurrentAdminUserId();
            try
            {
                // The service layer handles the business logic and audit logging
                var updatedUser = await _authService.UpdateUserStatus(adminUserId, targetUserId, newStatus);
                return Ok(updatedUser);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                // Log the exception details here in a real application
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Admin action: Blocks a user account.
        /// </summary>
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

        /// <summary>
        /// Admin action: Sends a custom message to a user.
        /// </summary>
        [HttpPost("user/message")]
        public async Task<IActionResult> SendCustomMessage([FromQuery] int recipientUserId, [FromBody] string content)
        {
            var adminUserId = GetCurrentAdminUserId();
            try
            {
                // The service layer handles message creation and audit logging
                await _authService.SendAdminMessage(adminUserId, recipientUserId, content);
                return Ok("Custom message sent and logged successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // --- CONTENT MODERATION ---

        /// <summary>
        /// Admin action: Publishes or unpublishes content.
        /// </summary>
        [HttpPost("content/publish")]
        public async Task<IActionResult> ModerateContent([FromQuery] int contentId, [FromBody] bool isPublished)
        {
            var adminUserId = GetCurrentAdminUserId();
            try
            {
                // The service layer handles the moderation logic and audit logging
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

        /// <summary>
        /// Admin action: Edits content on behalf of another user.
        /// </summary>
        [HttpPost("content/edit")]
        public async Task<IActionResult> EditContent([FromQuery] int contentId, [FromBody] ContentEditDto editDto)
        {
            var adminUserId = GetCurrentAdminUserId();
            try
            {
                // The service layer handles the editing logic and audit logging
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

        /// <summary>
        /// Admin action: Soft deletes content.
        /// </summary>
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

        // --- AUDIT LOGGING ---

        [HttpGet("audit/logs")]
        public async Task<IActionResult> GetAuditLogs([FromQuery] int page = 1, [FromQuery] int limit = 20)
        {
            var adminUserId = GetCurrentAdminUserId();
            
            // Retrieve and return audit logs for the current admin user
            var logs = await _context.AuditLogs
                .Where(l => l.AdminUserId == adminUserId && !l.IsDeleted)
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();
            
            return Ok(logs);
        }

        // Helper method to simulate getting the current admin user's ID from the claims
        private int GetCurrentAdminUserId()
        {
            // Best practice: Use a dedicated claim type for the user ID (e.g., "sub")
            if (User.Identity.IsAuthenticated && User.Claims.Any(c => c.Type == "sub"))
            {
                if (int.TryParse(User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value, out int userId))
                {
                    return userId;
                }
            }
            // Fallback for testing/simulation - should ideally throw an exception if not found
            return 1; 
        }
    }

    // DTO for content editing
    public class ContentEditDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new List<string>();
    }
}