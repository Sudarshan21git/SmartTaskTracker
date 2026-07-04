// Models/TaskItem.cs
namespace TaskTrackerFrontend.Models
{
    public class TaskItem
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int Priority { get; set; }       // 0 = Low, 1 = Medium, 2 = High

        public int Status { get; set; }         // 0 = Completed, 1 = InProgress, 2 = Pending (adjust if different)

        public DateTime CreatedAt { get; set; }

        public DateTime? DueDate { get; set; }

        public int UserId { get; set; }
    }
}