using System.ComponentModel.DataAnnotations;

namespace TaskTrackerAPI.Models.DTOs
{
    public class RegisterOrganizationRequest
    {
        [Required]
        public string OrganizationName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string CompanyEmail { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;

        public string? Phone { get; set; }
        public string? Address { get; set; }
    }
}