using System.ComponentModel.DataAnnotations;

namespace TaskTrackerFrontend.Models
{
    public class TaskCreateModel
    {
        [Required(ErrorMessage = "Task title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        [Display(Name = "Task Title")]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Priority is required")]
        [Display(Name = "Priority")]
        public int Priority { get; set; } = 1; // Default Medium

        [Required(ErrorMessage = "Due date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Due Date")]
        public DateTime DueDate { get; set; } = DateTime.Now.AddDays(1);

        [Range(1, 24 * 60, ErrorMessage = "Estimated time must be at least 1 minute")]
        [Display(Name = "Estimated Minutes")]
        public int EstimatedMinutes { get; set; } = 60;

        [Display(Name = "Status")]
        public int Status { get; set; } = 2; // Default Pending
    }
}