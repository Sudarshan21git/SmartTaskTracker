using System.ComponentModel.DataAnnotations;

namespace TaskTrackerAPI.Models
{
    public class CreateTaskRequest
    {
        [Required]
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? DueDate { get; set; }
        public TaskStatus Status { get; set; } = TaskStatus.Pending;
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
        [Required]
        public int UserId { get; set; }
    }

    public class UpdateTaskRequest
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public DateTime? DueDate { get; set; }
        public TaskStatus? Status { get; set; }
        public TaskPriority? Priority { get; set; }
    }

    public class UpdateTaskStatusRequest
    {
        [Required]
        public TaskStatus Status { get; set; }
    }
}
