using System.ComponentModel.DataAnnotations;

namespace TaskTrackerFrontend.Models
{
    public class RegisterModel
    {
        // ✅ NEW — Account type selector
        [Required]
        public string AccountType { get; set; } = "User";

        // ── INDIVIDUAL FIELDS ──
        [Display(Name = "First Name")]
        public string? FirstName { get; set; }

        [Display(Name = "Last Name")]
        public string? LastName { get; set; }

        // ── ORGANIZATION FIELDS ──
        [Display(Name = "Company Name")]
        public string? OrganizationName { get; set; }

        [Display(Name = "Phone")]
        public string? Phone { get; set; }

        [Display(Name = "Address")]
        public string? Address { get; set; }

        // ── SHARED FIELDS (both use these) ──
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d).+$",
            ErrorMessage = "Password must contain at least one capital letter and one number")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm Password is required")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}