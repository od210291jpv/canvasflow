using System.Collections.Generic;

namespace CanvasFlow.Api.Models
{
    public class Tag
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // Stored in lowercase
        public bool IsActive { get; set; } = true; // Soft delete/moderation
        
        // Navigation property for the many-to-many relationship
        public ICollection<Content> Contents { get; set; } = new List<Content>();
    }
}