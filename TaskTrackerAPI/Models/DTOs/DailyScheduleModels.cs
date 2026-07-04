using System.ComponentModel.DataAnnotations;

namespace TaskTrackerAPI.Models
{
    public class DailyScheduleRequest
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        [Range(1, 24 * 60)]
        public int AvailableMinutes { get; set; }
    }

    public class DailyScheduleResponse
    {
        public int UserId { get; set; }
        public int AvailableMinutes { get; set; }
        public int ScheduledMinutes { get; set; }
        public int RemainingMinutes { get; set; }
        public List<DailyScheduleItem> ScheduledTasks { get; set; } = new();
        public List<DailyScheduleItem> UnscheduledTasks { get; set; } = new();
    }

    public class DailyScheduleItem
    {
        public int TaskId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? DueDate { get; set; }
        public int Priority { get; set; }
        public int Status { get; set; }
        public int EstimatedMinutes { get; set; }
        public double Score { get; set; }
        public int PlannedOrder { get; set; }
    }
}