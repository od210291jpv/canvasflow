// Models/Message.cs
using System;

namespace CanvasFlow.Api.Models
{
    public class Message
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public int RecipientId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;
        public bool IsSystemNotification { get; set; } = false; // Differentiates between chat and alert
        public bool IsDeleted { get; set; } = false; // Soft Delete
    }
}