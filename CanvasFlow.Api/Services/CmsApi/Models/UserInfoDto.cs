namespace CanvasFlow.Api.Services.CmsApi.Models;

/// <summary>
/// User information returned from the login endpoint.
/// </summary>
public class UserInfoDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
