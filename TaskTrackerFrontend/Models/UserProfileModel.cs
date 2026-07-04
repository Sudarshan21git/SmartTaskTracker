using System.ComponentModel.DataAnnotations;

namespace TaskTrackerFrontend.Models
{
    public class UserProfileModel
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "First name is required")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;



        // Read-only properties
        public string FullName => $"{FirstName} {LastName}";
        public DateTime CreatedAt { get; set; }
        public string Role { get; set; } = string.Empty;
    }
}