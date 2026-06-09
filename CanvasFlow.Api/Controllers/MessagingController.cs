using CanvasFlow.Api.Hubs;
using CanvasFlow.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims; // �� �������� ��� using ��� ClaimTypes

namespace CanvasFlow.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class MessagingController : ControllerBase
    {
        private readonly IMessagingService _messagingService;
        private readonly INotificationService _notificationService;
        private readonly IHubContext<ChatHub> _chatHub;

        public MessagingController(IMessagingService messagingService, INotificationService notificationService, IHubContext<ChatHub> chatHub)
        {
            _messagingService = messagingService;
            _notificationService = notificationService;
            _chatHub = chatHub;
        }

        // ������: ����������� ����, ��� ASP.NET �� ��������� ��������� JSON { "Content": "�����" }
        public class SendMessageRequest
        {
            public string Content { get; set; } = string.Empty;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> SendChatMessage([FromQuery] int otherUserId, [FromBody] SendMessageRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest("Message content cannot be empty.");
            }

            var senderId = GetCurrentUserId();

            try
            {
                // 1. �������� �����������
                var message = await _messagingService.SendChatMessage(senderId, otherUserId, request.Content);

                // 2. ³���������� ����� SignalR
                // 2. ³  SignalR (      ID )
                await _chatHub.Clients.User(otherUserId.ToString()).SendAsync("ReceiveMessage", senderId, request.Content);

                // 3. ³���������� ���������
                await _notificationService.SendNotificationAsync(
                    recipientId: otherUserId,
                    senderId: senderId,
                    title: "New Chat Message",
                    content: "You received a new message.",
                    triggerType: "Chat"
                );

                return Ok(new { message.Id, message.Content, message.Timestamp });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

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

        // ==========================================
        // ������: �����, ����� �� ��������� (����� 404 Not Found)
        // ==========================================
        [HttpGet("inbox")]
        public async Task<IActionResult> GetInbox()
        {
            var currentUserId = GetCurrentUserId();

            try
            {
                // �������: � ������ IMessagingService �� ���� ����� GetUserInboxAsync, 
                // ���� ������� ������ ������ ��� ��������� �����������.
                var inbox = await _messagingService.GetUserInboxAsync(currentUserId);
                return Ok(inbox);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while fetching inbox: {ex.Message}");
            }
        }

        // ����������: ����� ������ �������� ID ����������� � ������ (�� � ContentController)
        private int GetCurrentUserId()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

            if (int.TryParse(userIdStr, out int userId))
            {
                return userId;
            }

            throw new UnauthorizedAccessException("User ID not found in token.");
        }
    }
}