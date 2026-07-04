using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskTrackerAPI.Models
{
    public class OrganizationTask
    {
        [Key]
        public int OrganizationTaskId { get; set; }

        [Required]
        public int OrganizationId { get; set; }

        [ForeignKey("OrganizationId")]
        public Organization? Organization { get; set; }

        [Required]
        public int CreatedByAdminId { get; set; }

        [ForeignKey("CreatedByAdminId")]
        public User? CreatedByAdmin { get; set; }

        public int? AssignedEmployeeId { get; set; }   

        [ForeignKey("AssignedEmployeeId")]
        public OrganizationEmployee? AssignedEmployee { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        public TaskStatus Status { get; set; } = TaskStatus.Pending;

        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        public DateTime? DueDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        public bool IsNotified { get; set; } = false;
    }
}