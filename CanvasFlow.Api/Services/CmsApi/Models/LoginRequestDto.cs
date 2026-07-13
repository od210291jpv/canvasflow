namespace CanvasFlow.Api.Services.CmsApi.Models;

/// <summary>
/// Request model for login endpoint.
/// </summary>
public class LoginRequestDto
{
    public string? Username { get; set; }
    public string? Password { get; set; }
}
