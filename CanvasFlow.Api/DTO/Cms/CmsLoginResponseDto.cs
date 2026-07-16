namespace CanvasFlow.Api.DTO.Cms
{
    public class CmsLoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public UserInfoDto User { get; set; } = new();
    }
}
