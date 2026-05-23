// Hubs/NotificationHub.cs
using Microsoft.AspNetCore.SignalR;

namespace CanvasFlow.Api.Hubs
{
    public class NotificationHub : Hub
    {
        // Clients connect using their User ID as the connection identifier.
        // The NotificationService will use this ID to target specific users.
        public override Task OnConnectedAsync()
        {
            // Optionally, perform connection-specific logic here (e.g., logging connection)
            return base.OnConnectedAsync();
        }
    }
}