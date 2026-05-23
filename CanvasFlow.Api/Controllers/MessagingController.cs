// Controllers/MessagingController.cs
using CanvasFlow.Api.Models;
using CanvasFlow.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CanvasFlow.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class MessagingController : ControllerBase
    {
        private readonly IMessagingService _messagingService;
        private readonly INotificationService _notificationService;

        public MessagingController(IMessagingService messagingService, INotificationService notificationService)
        {
            _messagingService = messagingService;
            _notificationService = notificationService;
        }

        /// <summary>
        /// Sends a private chat message to another user.
        /// </summary>
        /// <param name="otherUserId">The ID of the recipient.</param>
        /// <param name="messageDto">The message content.</param>
        [HttpPost("chat")]
        public async Task<IActionResult> SendChatMessage([FromQuery] int otherUserId, [FromBody] string messageDto)
        {
            if (string.IsNullOrWhiteSpace(messageDto))
            {
                return BadRequest("Message content cannot be empty.");
            }

            var senderId = GetCurrentUserId();
            
            try
            {
                // 1. Send the message
                var message = await _messagingService.SendChatMessage(senderId, otherUserId, messageDto);

                // 2. Notify the recipient in real-time
                await _notificationService.SendNotificationAsync(
                    recipientId: otherUserId,
                    senderId: senderId,
                    title: "New Chat Message",
                    content: $"You received a message from {User.Identity.Name}.", // Assuming username is in identity
                    triggerType: "Chat"
                );

                return Ok(new { message.Id, message.Content, message.Timestamp });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Retrieves the conversation history between the current user and another user.
        /// </summary>
        /// <param name="otherUserId">The ID of the user whose history is being viewed.</param>
        [HttpGet("history/{otherUserId}")]
        public async Task<IActionResult> GetConversationHistory(int otherUserId)
        {
            var currentUserId = GetCurrentUserId();
            
            if (currentUserId == otherUserId)
            {
                return BadRequest("Cannot view chat history with yourself.");
            }

            try
            {
                var history = await _messagingService.GetConversationHistory(currentUserId, otherUserId);
                return Ok(history);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while fetching history: {ex.Message}");
            }
        }

        // Helper method to simulate getting the current user's ID from the claims
        private int GetCurrentUserId()
        {
            // In a real application, we would extract the User ID from the JWT claims.
            // For this simulation, we assume the user ID is available in the claims.
            if (User.Identity.IsAuthenticated && User.Claims.Any(c => c.Type == "sub"))
            {
                if (int.TryParse(User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value, out int userId))
                {
                    return userId;
                }
            }
            // Fallback for testing/simulation
            return 1; 
        }
    }
}