using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskTrackerAPI.Models
{
    public class PersonalTask
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? DueDate { get; set; }

        public TaskStatus Status { get; set; } = TaskStatus.Pending;

        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        [Required]
        public int UserId { get; set; }

        public double? Score { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        //// ✅ New property for notification tracking
        public bool IsNotified { get; set; } = false;
    }
}

