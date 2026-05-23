// Services/IAuditService.cs
using CanvasFlow.Api.Models;

namespace CanvasFlow.Api.Services
{
    public interface IAuditService
    {
        /// <summary>
        /// Records a detailed audit log entry for administrative actions.
        /// </summary>
        /// <param name="adminUserId">The ID of the administrator performing the action.</param>
        /// <param name="action">A description of the action taken (e.g., "User Blocked").</param>
        /// <param name="targetEntity">The entity type affected (e.g., "User", "Content").</param>
        /// <param name="targetEntityId">The ID of the record affected.</param>
        /// <param name="details">Detailed JSON or string description of the change.</param>
        Task LogActionAsync(int adminUserId, string action, string targetEntity, int targetEntityId, string details);
    }
}