using CanvasFlow.Api.Data;
using CanvasFlow.Api.Models;
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

        public async Task<List<Content>> GetFeedAsync(int pageNumber, int pageSize)
        {
            // �������� ��������
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100; // ������ �� ������� ������� ������

            return await _context.Contents
                .Include(c => c.User) // ����������� ������
                .Include(c => c.Tags) // ����������� ����
                .Where(c => !c.IsDeleted && c.IsPublished) // ҳ���� ������������ � �� ��������� �������
                                                           // .OrderByDescending(c => c.CreatedAt) // �������������, ���� � ���� CreatedAt
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<Content> UploadContentAsync(int userId, string title, string description, string imageUrl, List<string> tags)
        {
            var content = new Content
            {
                UserId = userId,
                Title = title,
                Description = description,
                // ImageUrl = imageUrl, // �������������, ���� ������� ���� � ������ Content
                IsPublished = true, // �������� �� ���� �����-�����
                                    // CreatedAt = DateTime.UtcNow,
                Tags = new List<Tag>()
            };

            // ��������� ��������� ���� (�� �� ������ � EditContentAsAdmin)
            if (tags != null && tags.Any())
            {
                var newTagsLower = tags.Select(t => t.ToLowerInvariant()).ToList();
                var existingTags = await _context.Tags
                    .Where(t => newTagsLower.Contains(t.Name))
                    .ToListAsync();

                foreach (var tagName in newTagsLower)
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
            var content = await _context.Contents
                .FirstOrDefaultAsync(c => c.Id == contentId);

            if (content == null)
            {
                throw new KeyNotFoundException("Content not found.");
            }

            content.LikeCount++;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Content> UpdateContentAsync(int contentId, string title, string description, string imageUrl, List<string> tags)
        {
            var content = await _context.Contents
                .Include(c => c.Tags)
                .FirstOrDefaultAsync(c => c.Id == contentId);

            if (content == null)
            {
                throw new KeyNotFoundException("Content not found.");
            }

            // TODO: � ����� ���� ����� ���������� userId � ���������:
            // if (content.UserId != userId) throw new UnauthorizedAccessException();

            content.Title = title;
            content.Description = description;
            // content.ImageUrl = imageUrl; // ������������� ���� ��������� �����

            // ��������� ����
            content.Tags.Clear();

            if (tags != null && tags.Any())
            {
                var newTagsLower = tags.Select(t => t.ToLowerInvariant()).ToList();
                var existingTags = await _context.Tags
                    .Where(t => newTagsLower.Contains(t.Name))
                    .ToListAsync();

                foreach (var tagName in newTagsLower)
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
            // �������� _context.Contents.Update(content); ������� EF ��� ������� ����

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
            // ����������: ������ .Include(c => c.Tags), ��� �������� NullReferenceException
            var content = await _context.Contents
                .Include(c => c.Tags)
                .FirstOrDefaultAsync(c => c.Id == contentId);

            if (content == null)
            {
                throw new KeyNotFoundException("Content not found.");
            }

            content.Title = newTitle;
            content.Description = newDescription;

            // ����� �� ��������, �� Tags �����������
            content.Tags.Clear();

            if (newTags != null && newTags.Any())
            {
                // ��������� �� ������ ���� �� �������� ������� ������
                var newTagsLower = newTags.Select(t => t.ToLowerInvariant()).ToList();

                // ����������: �������� �� ������� ���� � ���� ����� ������� ������ ������ � ����
                var existingTags = await _context.Tags
                    .Where(t => newTagsLower.Contains(t.Name))
                    .ToListAsync();

                foreach (var tagName in newTagsLower)
                {
                    var tagToAssign = existingTags.FirstOrDefault(t => t.Name == tagName);

                    if (tagToAssign == null)
                    {
                        tagToAssign = new Tag { Name = tagName };
                        // �������� SaveChangesAsync � �����. EF ������� ����� ��� ����������� �� ��� ���������� ����������
                    }

                    content.Tags.Add(tagToAssign);
                }
            }

            // �������� ������ _context.Contents.Update(content);
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
    }
}