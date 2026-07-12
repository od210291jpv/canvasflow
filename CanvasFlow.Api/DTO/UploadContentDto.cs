namespace CanvasFlow.Api.DTO
{
    public class UploadContentDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Microsoft.AspNetCore.Http.IFormFile? File { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
    }
}