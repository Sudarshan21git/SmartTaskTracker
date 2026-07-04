using System.ComponentModel.DataAnnotations;

namespace TaskTrackerFrontend.Models
{
    public class CreateEmployeeModel
    {
        [Required(ErrorMessage = "First name is required")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Department")]
        public string? Department { get; set; }

        [Display(Name = "Position")]
        public string? Position { get; set; }
    }

    public class CreateEmployeeResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public string LoginEmail { get; set; } = string.Empty;
        public string TempPassword { get; set; } = string.Empty;
    }
}