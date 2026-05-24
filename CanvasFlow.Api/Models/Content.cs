namespace CanvasFlow.Api.Models
{
    public class Content
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty; // URL to the uploaded art
        
        public ICollection<Tag> Tags { get; set; } = new List<Tag>(); 
        
        public DateTime UploadDate { get; set; } = DateTime.UtcNow;
        public bool IsPublished { get; set; } = true; // For moderation
        public bool IsDeleted { get; set; } = false; // Soft Delete
        public int LikeCount { get; set; } = 0;
    }
}