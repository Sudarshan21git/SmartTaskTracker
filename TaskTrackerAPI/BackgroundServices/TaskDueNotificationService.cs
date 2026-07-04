using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TaskTrackerAPI.Data;
using TaskTrackerAPI.Hubs;


namespace TaskTrackerAPI.BackgroundServices
{
    public class TaskDueNotificationService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<TaskNotificationHub> _hubContext;

        public TaskDueNotificationService(
            IServiceScopeFactory scopeFactory,
            IHubContext<TaskNotificationHub> hubContext)
        {
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Loop until the app shuts down
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                // Get all tasks due today or tomorrow that haven't been notified
                var tasks = await db.Tasks
                    .Where(t => (t.DueDate == today || t.DueDate == tomorrow)
                                && !t.IsNotified)
                    .ToListAsync();

                foreach (var task in tasks)
                {
                    // Send notification to user via SignalR
                    await _hubContext.Clients
                        .Group(task.UserId.ToString())
                        .SendAsync("ReceiveNotification", new
                        {
                            title = "Task Reminder",
                            message = $"Task '{task.Title}' is due on {task.DueDate:dd MMM}"
                        });

                    // Mark task as notified to prevent duplicate notifications
                    task.IsNotified = true;
                }

                await db.SaveChangesAsync();

                // Wait 5 minutes before checking again
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
