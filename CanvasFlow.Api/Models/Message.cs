// Models/Message.cs
using System;

namespace CanvasFlow.Api.Models
{
    public enum MessageType
    {
        Chat, // User-initiated, two-way conversation
        SystemNotification, // One-way, read-only alert (e.g., 'User X liked your art')
        AdminMessage // One-way, read-only message from Admin
    }

    public class Message
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public int RecipientId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;
        public MessageType Type { get; set; } = MessageType.Chat; // Use enum for clarity
        public bool IsDeleted { get; set; } = false; // Soft Delete
    }
}