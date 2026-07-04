// Services/INotificationService.cs
using CanvasFlow.Api.Models;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CanvasFlow.Api.Services
{
    public interface INotificationService
    {
        /// <summary>
        /// Sends a system notification to a specific user.
        /// </summary>
        /// <param name="recipientId">The ID of the user receiving the notification.</param>
        /// <param name="senderId">The ID of the user/system generating the notification.</param>
        /// <param name="title">The title of the notification.</param>
        /// <param name="content">The detailed content of the notification.</param>
        /// <param name="triggerType">The type of event that triggered the notification (e.g., Like, ContactAdd).</param>
        Task SendNotificationAsync(int recipientId, int senderId, string title, string content, string triggerType);

        /// <summary>
        /// Broadcasts a notification to all connected clients (useful for global alerts).
        /// </summary>
        Task BroadcastNotificationAsync(string title, string content);

        /// <summary>
        /// Retrieves all system notifications for a specific user.
        /// </summary>
        /// <param name="userId">The ID of the user whose notifications are being retrieved.</param>
        /// <returns>A collection of messages filtered by SystemNotification type.</returns>
        Task<IEnumerable<Message>> GetUserNotificationsAsync(int userId);
    }
}