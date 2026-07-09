using CanvasFlow.Db.Data;
using CanvasFlow.Db.Models;
using Microsoft.EntityFrameworkCore;

namespace CanvasFlow.Api.Services
{
    public class ContentService : IContentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
 
        public ContentService(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }
 
        public async Task<Content> GetContentByIdAsync(int contentId)
        {
            return await _context.Contents
                .Include(c => c.Tags)
                .FirstOrDefaultAsync(c => c.Id == contentId);
        }
 
        public async Task<List<Content>> GetFeedAsync(int pageNumber, int pageSize, List<string> tags = null)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;
 
            IQueryable<Content> query = _context.Contents
                .Include(c => c.User)
                .Include(c => c.Tags)
                .Where(c => !c.IsDeleted && c.IsPublished);
 
            if (tags != null && tags.Any())
            {
                // AND intersection: content must have ALL provided tags
                var tagIds = await _context.Tags
                    .Where(t => tags.Contains(t.Name))
                    .Select(t => t.Id)
                    .ToListAsync();
 
                if (tagIds.Any())
                {
                    // Filter contents that contain all the required tag IDs
                    query = query.Where(c => c.Tags.Any(t => tagIds.Contains(t.Id)) && 
                                             c.Tags.Count(t => tagIds.Contains(t.Id)) == tagIds.Count);
                }
                else
                {
                    // If none of the provided tags exist in DB, return empty list
                    return new List<Content>();
                }
            }
 
            return await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
 
        public async Task<Content> UploadContentAsync(int userId, string title, string description, string imageUrl, List<string> tags)
        {
            var normalizedTags = NormalizeTags(tags);
 
            var content = new Content
            {
                UserId = userId,
                Title = title,
                Description = description,
                ImageUrl = imageUrl,
                IsPublished = true,
                Tags = new List<Tag>()
            };
 
            if (normalizedTags.Any())
            {
                var existingTags = await _context.Tags
                    .Where(t => normalizedTags.Contains(t.Name))
                    .ToListAsync();
 
                foreach (var tagName in normalizedTags)
                {
                    var tagToAssign = existingTags.FirstOrDefault(t => t.Name == tagName) ?? new Tag { Name = tagName };
                    content.Tags.Add(tagToAssign);
                }
            }
 
            _context.Contents.Add(content);
            await _context.SaveChangesAsync();
 
            return content;
        }
 
        public async Task<bool> LikeContentAsync(int contentId, int userId)
        {
            // Use ExecuteUpdateAsync for atomic increment to prevent race conditions
            int rowsAffected = await _context.Contents
                .Where(c => c.Id == contentId)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.LikeCount, c => c.LikeCount + 1));
 
            return rowsAffected > 0;
        }
 
        public async Task<Content> UpdateContentAsync(int contentId, string title, string description, List<string> tags)
        {
            var content = await _context.Contents
                .Include(c => c.Tags)
                .FirstOrDefaultAsync(c => c.Id == contentId);
 
            if (content == null)
            {
                throw new KeyNotFoundException("Content not found.");
            }
 
            content.Title = title;
            content.Description = description;
            content.Tags.Clear();
 
            var normalizedTags = NormalizeTags(tags);
 
            if (normalizedTags.Any())
            {
                var existingTags = await _context.Tags
                    .Where(t => normalizedTags.Contains(t.Name))
                    .ToListAsync();
 
                foreach (var tagName in normalizedTags)
                {
                    var tagToAssign = existingTags.FirstOrDefault(t => t.Name == tagName) ?? new Tag { Name = tagName };
                    content.Tags.Add(tagToAssign);
                }
            }
 
            await _context.SaveChangesAsync();
 
            return content;
        }
 
        public async Task<Content> ModerateContentAsync(int adminUserId, int contentId, bool isPublished)
        {
            var content = await _context.Contents.FindAsync(contentId);
            if (content == null)
            {
                throw new KeyNotFoundException("Content not found.");
            }
 
            content.IsPublished = isPublished;
 
            await _context.SaveChangesAsync();
 
            await _auditService.LogActionAsync(
                adminUserId,
                "Content Moderation",
                "Content",
                contentId,
                $"Content visibility set to {isPublished} by Admin."
            );
 
            return content;
        }
 
        public async Task<Content> EditContentAsAdminAsync(int adminUserId, int contentId, string newTitle, string newDescription, List<string> newTags)
        {
            var content = await _context.Contents
                .Include(c => c.Tags)
                .FirstOrDefaultAsync(c => c.Id == contentId);
 
            if (content == null)
            {
                throw new KeyNotFoundException("Content not found.");
            }
 
            content.Title = newTitle;
            content.Description = newDescription;
 
            content.Tags.Clear();
 
            var normalizedTags = NormalizeTags(newTags);
 
            if (normalizedTags.Any())
            {
                var existingTags = await _context.Tags
                    .Where(t => normalizedTags.Contains(t.Name))
                    .ToListAsync();
 
                foreach (var tagName in normalizedTags)
                {
                    var tagToAssign = existingTags.FirstOrDefault(t => t.Name == tagName);
 
                    if (tagToAssign == null)
                    {
                        tagToAssign = new Tag { Name = tagName };
                    }
 
                    content.Tags.Add(tagToAssign);
                }
            }
 
            await _context.SaveChangesAsync();
 
            await _auditService.LogActionAsync(
                adminUserId,
                "Content Edited by Admin",
                "Content",
                contentId,
                $"Title updated to '{newTitle}'. Tags updated."
            );
 
            return content;
        }
 
        public async Task<List<Content>> GetContentByUserIdAsync(int userId)
        {
            var result = await _context.Contents
                .Include(c => c.Tags)
                .Where(c => c.UserId == userId && !c.IsDeleted)
                .OrderByDescending(c => c.UploadDate)
                .ToListAsync();
            return result;
        }

        public async Task<bool> DeleteContentAsync(int adminUserId, int contentId)
        {
            var content = await _context.Contents.FindAsync(contentId);
            if (content == null)
            {
                throw new KeyNotFoundException("Content not found.");
            }

            content.IsDeleted = true;

            int result = await _context.SaveChangesAsync();

            await _auditService.LogActionAsync(
                adminUserId,
                "Content Deletion",
                "Content",
                contentId,
                "Content was soft-deleted by Admin."
            );

            return result > 0;
        }

        public async Task<List<string>> GetAllTagsAsync()
        {
            return await _context.Tags
                .Select(t => t.Name)
                .Distinct()
                .OrderBy(n => n)
                .ToListAsync();
        }
 
        private List<string> NormalizeTags(List<string> tags, int maxLimit = 10)
        {
            if (tags == null || !tags.Any()) return new List<string>();
 
            var normalized = tags
                .Select(t => t.Trim().ToLowerInvariant())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct()
                .Take(maxLimit)
                .ToList();
 
            return normalized;
        }
    }
}