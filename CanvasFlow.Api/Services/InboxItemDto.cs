// Services/IMessagingService.cs
namespace CanvasFlow.Api.Services
{
    public class InboxItemDto
    {
        public int OtherUserId { get; set; }
        public string OtherUserName { get; set; } = string.Empty;
        public string LastMessage { get; set; } = string.Empty;
        public DateTime LastMessageTimestamp { get; set; }
        public bool HasUnread { get; set; }
    }
}