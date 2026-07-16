using CanvasFlow.Db.Models;

namespace CanvasFlow.Api.Services
{
    public interface IContentService
    {
        Task<Content> GetContentByIdAsync(int contentId);
 
        Task<Content> ModerateContentAsync(int adminUserId, int contentId, bool isPublished);
 
        Task<Content> EditContentAsAdminAsync(int adminUserId, int contentId, string newTitle, string newDescription, List<string> newTags);
 
        // ����������: ������ adminUserId ��� ������, �������� ����� ��������
        Task<bool> DeleteContentAsync(int adminUserId, int contentId);
 
        Task<List<Content>> GetFeedAsync(int pageNumber, int pageSize, List<string> tags = null);
 
        Task<int> GetFeedCountAsync(List<string> tags = null);
 
        Task<Content> UploadContentAsync(int userId, string title, string description, string imageUrl, List<string> tags);
 
        Task<bool> LikeContentAsync(int contentId, int userId);
 
        Task<Content> UpdateContentAsync(int contentId, string title, string description, List<string> tags);
 
        Task<List<Content>> GetContentByUserIdAsync(int userId);

        Task<List<string>> GetAllTagsAsync();
    }
}