using System.ComponentModel.DataAnnotations;

namespace TaskTrackerFrontend.Models
{
    public class UpdateStatusRequest
    {
        [Required]
        public int TaskId { get; set; }

        [Required]
        public string NewStatus { get; set; } = string.Empty; // e.g., "Pending", "InProgress", "Completed"
    }
}