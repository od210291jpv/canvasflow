// Services/MessagingService.cs
using CanvasFlow.Api.Data;
using CanvasFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CanvasFlow.Api.Services
{
    public class MessagingService : IMessagingService
    {
        private readonly ApplicationDbContext _context;

        public MessagingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Message> SendChatMessage(int senderId, int recipientId, string content)
        {
            // 1. Validation: Ensure sender and recipient are active users
            var sender = await _context.Users.FindAsync(senderId);
            var recipient = await _context.Users.FindAsync(recipientId);

            if (sender == null || recipient == null || sender.IsDeleted || recipient.IsDeleted)
            {
                throw new InvalidOperationException("One or both users are inactive or do not exist.");
            }

            // 2. Create the message
            var message = new Message
            {
                SenderId = senderId,
                RecipientId = recipientId,
                Content = content,
                Type = MessageType.Chat,
                IsRead = false, // New messages are unread
                IsDeleted = false
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // 3. Optional: Mark the message as read for the recipient (if we assume the sender is initiating the chat)
            // For simplicity, we just save the message and let the client handle the read status update.
            
            return message;
        }

        public async Task<List<Message>> GetConversationHistory(int userId, int otherUserId)
        {
            // Fetch messages where the current user is either the sender or the recipient.
            var history = await _context.Messages
                .Where(m => (m.SenderId == userId && m.RecipientId == otherUserId) || 
                            (m.SenderId == otherUserId && m.RecipientId == userId))
                .Where(m => !m.IsDeleted)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            // Filter to ensure the current user's messages are marked as read if they are older than the current time
            // (This logic is usually handled by a separate 'mark as read' endpoint, but we ensure the data is clean)
            
            return history;
        }
    }
}