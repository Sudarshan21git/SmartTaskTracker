using System.ComponentModel.DataAnnotations;

namespace TaskTrackerFrontend.Models
{
    public class TaskUpdateModel
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "Task title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Priority is required")]
        public int Priority { get; set; }

        [Required(ErrorMessage = "Due date is required")]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        [Required(ErrorMessage = "Status is required")]
        public int Status { get; set; }

        // For completion tracking
        public DateTime? CompletedAt { get; set; }

        public bool IsCompleted => Status == 0;
    }
}