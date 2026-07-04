using TaskTrackerFrontend.Models;
public class DashboardViewModel
{
    public int UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserRole { get; set; } = "User";

    // Task lists
    public List<TaskItem> RecentTasks { get; set; } = new List<TaskItem>();
    public List<TaskItem> UpcomingTasks { get; set; } = new List<TaskItem>();

    // Statistics
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int PendingTasks { get; set; }
    public int InProgressTasks { get; set; }
    public int OverdueTasks { get; set; }

    // Priority counts - these are likely already there
    public int HighPriorityTasks { get; set; }
    public int MediumPriorityTasks { get; set; }

    // ← ADD THIS LINE:
    public int LowPriorityTasks { get; set; }
}