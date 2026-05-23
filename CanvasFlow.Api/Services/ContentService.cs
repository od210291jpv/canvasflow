using CanvasFlow.Api.Data;
using CanvasFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CanvasFlow.Api.Services
{
    public class ContentService : IContentService
    {
        private readonly ApplicationDbContext _context;

        public ContentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Content>> GetFeedAsync(int page, int limit, List<string>? tags = null)
        {
            IQueryable<Content> query = _context.Contents
                .Include(c => c.User)
                .Include(c => c.Tags)
                .Where(c => !c.IsDeleted);

            if (tags != null && tags.Any())
            {
                // Filter by tags
                query = query.Where(c => c.Tags.Any(t => tags.Contains(t.Name)));
            }

            // Додаємо сортування в самому кінці
            var items = await query
                .OrderByDescending(c => c.UploadDate)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            return items;
        }

        public async Task<Content> UploadContentAsync(int userId, string title, string description, string imageUrl, List<string> tags)
        {
            var tagNames = tags.Select(t => t.ToLower()).Distinct().ToList();

            var existingTags = await _context.Tags.Where(t => tagNames.Contains(t.Name)).ToListAsync();

            // 2. Create new tags if they don't exist (ensuring lowercase)
            var tagsToCreate = new List<Tag>();
            foreach (var tagName in tagNames)
            {
                var existingTag = existingTags.FirstOrDefault(t => t.Name == tagName);
                if (existingTag == null)
                {
                    tagsToCreate.Add(new Tag { Name = tagName });
                }
            }

            // Bulk add new tags
            if (tagsToCreate.Any())
            {
                _context.Tags.AddRange(tagsToCreate);
                await _context.SaveChangesAsync();
            }

            // Re-fetch all tags to include newly created ones
            var allTags = await _context.Tags.Where(t => tagNames.Contains(t.Name)).ToListAsync();

            // 3. Create Content
            var newContent = new Content
            {
                UserId = userId,
                Title = title,
                Description = description,
                ImageUrl = imageUrl,
                Tags = allTags, // Assign all relevant tags
                IsPublished = true,
                UploadDate = DateTime.UtcNow // Бажано додавати дату
            };

            _context.Contents.Add(newContent);
            await _context.SaveChangesAsync();

            return newContent;
        }

        public async Task<bool> LikeContentAsync(int contentId, int userId)
        {
            var content = await _context.Contents.FindAsync(contentId);
            if (content == null) return false;

            // БОНУСНЕ ВИПРАВЛЕННЯ: Відповідно до вимог (2.8), користувач не може лайкати власний контент
            if (content.UserId == userId) return false;

            // Basic check: Prevent a user from liking the same content multiple times
            content.LikeCount++;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Content> UpdateContentAsync(int contentId, string title, string description, string imageUrl, List<string> tags)
        {
            var content = await _context.Contents
                .Include(c => c.Tags)
                .FirstOrDefaultAsync(c => c.Id == contentId);

            if (content == null) throw new KeyNotFoundException("Content not found.");

            // Update metadata
            content.Title = title;
            content.Description = description;
            content.ImageUrl = imageUrl;

            var tagNames = tags.Select(t => t.ToLower()).Distinct().ToList();
            var existingTags = await _context.Tags.Where(t => tagNames.Contains(t.Name)).ToListAsync();

            content.Tags.Clear();
            foreach (var tag in existingTags)
            {
                content.Tags.Add(tag);
            }

            await _context.SaveChangesAsync();
            return content;
        }

        public async Task<bool> DeleteContentAsync(int contentId, int userId)
        {
            var content = await _context.Contents.FindAsync(contentId);
            if (content == null || content.UserId != userId) return false;

            // Soft delete
            content.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}