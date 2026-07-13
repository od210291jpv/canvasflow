using CanvasFlow.Db.Data;
using CanvasFlow.Db.Models;
using Microsoft.EntityFrameworkCore;

namespace CanvasFlow.Api.Services
{
    public class CmsContentService : ContentService
    {
        public CmsContentService(ApplicationDbContext context, IAuditService auditService) : base(context, auditService)
        {
        }        
    }
}
