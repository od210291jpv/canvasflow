// Services/AuditService.cs
using CanvasFlow.Api.Data;
using CanvasFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CanvasFlow.Api.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;

        public AuditService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogActionAsync(int adminUserId, string action, string targetEntity, int targetEntityId, string details)
        {
            var log = new AuditLog
            {
                AdminUserId = adminUserId,
                Action = action,
                TargetEntity = targetEntity,
                TargetEntityId = targetEntityId,
                Details = details,
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}