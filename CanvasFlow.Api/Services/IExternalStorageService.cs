namespace CanvasFlow.Api.Services
{
    public interface IExternalStorageService
    {
        Task UploadImageAsync(byte[] fileBytes, string fileName, string contentType);

        Task<(Stream? Stream, string ContentType)> GetImageStreamAsync(string fileName);
        
        Task<string> GetExternalImagesJsonAsync();
    }
}