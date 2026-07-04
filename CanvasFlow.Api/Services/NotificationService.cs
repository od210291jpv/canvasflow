// Services/NotificationService.cs
using CanvasFlow.Api.Data;
using CanvasFlow.Api.Hubs;
using CanvasFlow.Api.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CanvasFlow.Api.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(ApplicationDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task SendNotificationAsync(int recipientId, int senderId, string title, string content, string triggerType)
        {
            // 1. Create the persistent system notification record
            var notification = new Message
            {
                SenderId = senderId,
                RecipientId = recipientId,
                Content = $"{title}: {content} (Trigger: {triggerType})",
                Type = MessageType.SystemNotification,
                IsRead = false,
                Timestamp = DateTime.UtcNow
            };

            _context.Messages.Add(notification);
            await _context.SaveChangesAsync();

            // 2. Real-time push via SignalR
            await _hubContext.Clients.User(recipientId.ToString()).SendAsync("ReceiveNotification", new 
            {
                SenderId = senderId,
                RecipientId = recipientId,
                Content = $"{title}: {content}",
                Type = MessageType.SystemNotification,
                IsRead = false,
                Timestamp = DateTime.UtcNow
            });
        }

        public async Task BroadcastNotificationAsync(string title, string content)
        {
            // Broadcast to all connected users
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", new Message
            {
                SenderId = 0, // System
                RecipientId = 0, // System
                Content = $"{title}: {content}",
                Type = MessageType.SystemNotification,
                IsRead = false,
                Timestamp = DateTime.UtcNow
            });
        }

        public async Task<IEnumerable<Message>> GetUserNotificationsAsync(int userId)
        {
            // Retrieve only system notifications for the specific user, ordered by newest first
            return await _context.Messages
                .Where(m => m.RecipientId == userId && m.Type == MessageType.SystemNotification)
                .OrderByDescending(m => m.Timestamp)
                .ToListAsync();
        }
    }
}
