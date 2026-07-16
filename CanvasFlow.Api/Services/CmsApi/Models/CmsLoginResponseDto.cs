namespace CanvasFlow.Api.Services.CmsApi.Models;

/// <summary>
/// Response model from the login endpoint.
/// </summary>
public class CmsLoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public UserInfoDto? User { get; set; }
}
