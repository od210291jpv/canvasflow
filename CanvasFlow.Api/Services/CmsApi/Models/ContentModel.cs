namespace CanvasFlow.Api.Services.CmsApi.Models;

/// <summary>
/// Full content model with owner navigation property.
/// </summary>
public class ContentModel
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    public UserModel? Owner { get; set; }
    public bool Enabled { get; set; }
    public string? Description { get; set; }
    public string Path { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
