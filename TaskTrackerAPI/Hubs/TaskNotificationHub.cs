using Microsoft.AspNetCore.SignalR;

namespace TaskTrackerAPI.Hubs
{
    public class TaskNotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            // Use NameIdentifier claim for user ID
            var userId = Context.UserIdentifier;

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            }

            await base.OnConnectedAsync();
        }
    }
}
