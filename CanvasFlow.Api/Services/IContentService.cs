using CanvasFlow.Api.Models;
using CanvasFlow.Api.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CanvasFlow.Api.Services
{
    public interface IContentService
    {
        /// <summary>
        /// Retrieves the main feed content, filtered by tags and pagination.
        /// </summary>
        Task<IEnumerable<Content>> GetFeedAsync(int page, int limit, List<string>? tags = null);

        /// <summary>
        /// Uploads new content, validates metadata, and saves it to the database.
        /// </summary>
        Task<Content> UploadContentAsync(int userId, string title, string description, string imageUrl, List<string> tags);

        /// <summary>
        /// Increments the like count for a piece of content.
        /// </summary>
        Task<bool> LikeContentAsync(int contentId, int userId);

        /// <summary>
        /// Allows the user to edit their own content.
        /// </summary>
        Task<Content> UpdateContentAsync(int contentId, string title, string description, string imageUrl, List<string> tags);

        /// <summary>
        /// Allows the user to delete their own content (soft delete).
        /// </summary>
        Task<bool> DeleteContentAsync(int contentId, int userId);
    }
}