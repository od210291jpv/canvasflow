// Models/AuditLog.cs
using System;

namespace CanvasFlow.Api.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        public int AdminUserId { get; set; } // The user who performed the action
        public string Action { get; set; } = string.Empty; // e.g., "User Blocked", "Content Unpublished"
        public string TargetEntity { get; set; } = string.Empty; // e.g., "User", "Content"
        public int TargetEntityId { get; set; } // ID of the record affected
        public string Details { get; set; } = string.Empty; // JSON or detailed description of the change
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false; // Soft Delete
    }
}