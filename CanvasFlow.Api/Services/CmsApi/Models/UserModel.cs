namespace CanvasFlow.Api.Services.CmsApi.Models;

/// <summary>
/// User model returned from the CMS API.
/// </summary>
public class UserModel
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public List<ContentModel>? OwnedContent { get; set; }
    public DateTime CreatedAt { get; set; }
}
