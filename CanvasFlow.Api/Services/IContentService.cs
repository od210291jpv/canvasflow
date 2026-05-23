using CanvasFlow.Api.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CanvasFlow.Api.Services
{
    public interface IContentService
    {
        Task<Content> GetContentByIdAsync(int contentId);

        Task<Content> ModerateContentAsync(int adminUserId, int contentId, bool isPublished);

        Task<Content> EditContentAsAdminAsync(int adminUserId, int contentId, string newTitle, string newDescription, List<string> newTags);

        // Виправлено: додано adminUserId для аудиту, видалено зайву заглушку
        Task<bool> DeleteContentAsync(int adminUserId, int contentId);

        Task<List<Content>> GetFeedAsync(int pageNumber, int pageSize);

        Task<Content> UploadContentAsync(int userId, string title, string description, string imageUrl, List<string> tags);

        Task<bool> LikeContentAsync(int contentId, int userId);

        Task<Content> UpdateContentAsync(int contentId, string title, string description, string imageUrl, List<string> tags);
    }
}