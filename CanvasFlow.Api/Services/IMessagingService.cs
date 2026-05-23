// Services/IMessagingService.cs
using CanvasFlow.Api.Models;

namespace CanvasFlow.Api.Services
{
    public interface IMessagingService
    {
        /// <summary>
        /// Sends a private chat message between two users.
        /// </summary>
        /// <param name="senderId">The ID of the user sending the message.</param>
        /// <param name="recipientId">The ID of the user receiving the message.</param>
        /// <param name="content">The message content.</param>
        /// <returns>The created message object.</returns>
        Task<Message> SendChatMessage(int senderId, int recipientId, string content);

        /// <summary>
        /// Retrieves the conversation history for a user with another user.
        /// </summary>
        /// <param name="userId">The ID of the current user.</param>
        /// <param name="otherUserId">The ID of the user whose history is being viewed.</param>
        /// <returns>A list of messages, sorted by timestamp.</returns>
        Task<List<Message>> GetConversationHistory(int userId, int otherUserId);
    }
}