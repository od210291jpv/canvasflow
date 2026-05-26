using CanvasFlow.Api.Data;
using CanvasFlow.Api.Models;
using CanvasFlow.Api.Models.Enums;
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

            if (senderId == recipientId)
            {
                throw new InvalidOperationException("Cannot send a message to yourself.");
            }

            if (sender == null || recipient == null || 
                sender.IsDeleted || recipient.IsDeleted || 
                sender.AccountStatus != UserStatus.Active || 
                recipient.AccountStatus != UserStatus.Active)
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

        public async Task<List<InboxItemDto>> GetUserInboxAsync(int userId)
        {
            // 1. �������� �� �����������, �� ���������� � ����������� ��� �����������,
            // ����������� �� ��������� �� ����������.
            var messages = await _context.Messages
                .Where(m => !m.IsDeleted && (m.SenderId == userId || m.RecipientId == userId))
                .OrderByDescending(m => m.Timestamp)
                .ToListAsync();

            // 2. ������� ����������� �� ID ������ ����������� (�������������)
            var groupedMessages = messages.GroupBy(m => m.SenderId == userId ? m.RecipientId : m.SenderId);

            // 3. �������� ����� ������������-������������� ����� ������� �� ��
            var contactIds = groupedMessages.Select(g => g.Key).ToList();

            // �������: �������������, �� DbSet ���������� Users � ������ ApplicationDbContext
            var contacts = await _context.Users
                .Where(u => contactIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Username);

            var inbox = new List<InboxItemDto>();

            // 4. ������� ��������� ������ ��� Inbox
            foreach (var group in groupedMessages)
            {
                var contactId = group.Key;
                var latestMessage = group.First(); // ������� �� ��� ����������� �� ����

                // ����������, �� � ���� � ���� ����������� �����������, �������� ��� �� ����� ��������
                var hasUnread = group.Any(m => m.SenderId == contactId && m.RecipientId == userId && !m.IsRead);

                inbox.Add(new InboxItemDto
                {
                    OtherUserId = contactId,
                    OtherUserName = contacts.ContainsKey(contactId) ? contacts[contactId] : "Unknown User",
                    LastMessage = latestMessage.Content,
                    LastMessageTimestamp = latestMessage.Timestamp,
                    HasUnread = hasUnread
                });
            }

            // ��������� ������, ������������ �� ����� ���������� �����������
            return inbox.OrderByDescending(i => i.LastMessageTimestamp).ToList();
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

            // Mark incoming messages as read when the history is viewed
            var unreadMessages = history.Where(m => m.RecipientId == userId && !m.IsRead).ToList();
            if (unreadMessages.Any())
            {
                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true;
                }
                await _context.SaveChangesAsync();
            }
            
            return history;
        }
    }
}