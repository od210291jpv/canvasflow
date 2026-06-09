using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CanvasFlow.Api.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        // Users join a group based on their User ID so you can target them directly
        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            }
            await base.OnConnectedAsync();
        }
    }
}