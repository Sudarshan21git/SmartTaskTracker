using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskTrackerAPI.Models
{
    public class Organization
    {
        [Key]
        public int OrganizationId { get; set; }

        [Required, StringLength(150)]
        public string OrganizationName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(100)]
        public string CompanyEmail { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(250)]
        public string? Address { get; set; }

        [Required]
        public int AdminUserId { get; set; }

        [ForeignKey("AdminUserId")]
        public User? AdminUser { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public List<OrganizationEmployee> Employees { get; set; } = new();
        public List<OrganizationTask> OrganizationTasks { get; set; } = new();
    }
}