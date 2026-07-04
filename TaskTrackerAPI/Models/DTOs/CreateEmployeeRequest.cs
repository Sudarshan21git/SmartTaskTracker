using System.ComponentModel.DataAnnotations;

namespace TaskTrackerAPI.Models.DTOs
{
    public class CreateEmployeeRequest
    {
        
            [Required] public string FirstName { get; set; } = string.Empty;
            [Required] public string LastName { get; set; } = string.Empty;
            [Required, EmailAddress]
            public string Email { get; set; } = string.Empty;
            public string? Department { get; set; }
            public string? Position { get; set; }
        
    }
}
