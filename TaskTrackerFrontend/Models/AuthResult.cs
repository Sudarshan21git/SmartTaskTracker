namespace TaskTrackerFrontend.Models
{
    public class AuthResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string Role { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;


        public int OrganizationId { get; set; }
        public bool MustChangePassword { get; set; } = false;
        public string? EmployeeCode { get; set; }
        public string? Department { get; set; }
    }
}