using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskTrackerAPI.Data;
using TaskTrackerAPI.Models;
using TaskTrackerAPI.Models.DTOs;
using TaskTrackerAPI.Services;

namespace TaskTrackerAPI.Controllers
{
    [ApiController]
    [Route("api/organization")]
    [Authorize(Roles = "Organization")]
    public class OrganizationController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IEmailService _emailService;

        public OrganizationController(AppDbContext db, IEmailService emailService)
        {
            _db = db;
            _emailService = emailService;
        }

        // Helper — get current org from JWT
        private async Task<Organization?> GetMyOrg()
        {
            var adminId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            return await _db.Organizations
                .FirstOrDefaultAsync(o => o.AdminUserId == adminId);
        }

        // POST: api/organization/create-employee
        [HttpPost("create-employee")]
        public async Task<IActionResult> CreateEmployee(
            [FromBody] CreateEmployeeRequest request)
        {
            var org = await GetMyOrg();
            if (org == null)
                return NotFound(new { message = "Organization not found." });

            var adminId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var email = request.Email.Trim().ToLower();

            // Check email not already used
            if (await _db.Users.AnyAsync(u => u.Email == email) ||
                await _db.OrganizationEmployees.AnyAsync(e => e.Email == email))
            {
                return BadRequest(new { message = "Email already in use." });
            }

            // Generate temp password
            var tempPassword = "Emp@" + new Random().Next(1000, 9999);

            // Generate employee code
            var orgPrefix = org.OrganizationName.Length >= 3
                ? org.OrganizationName[..3].ToUpper()
                : org.OrganizationName.ToUpper();

            var empCode = orgPrefix + new Random().Next(100, 999).ToString();

            var employee = new OrganizationEmployee
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword),
                EmployeeCode = empCode,
                Department = request.Department,
                Position = request.Position,
                OrganizationId = org.OrganizationId,
                CreatedByAdminId = adminId,
                IsActive = true,
                MustChangePassword = true
            };

            _db.OrganizationEmployees.Add(employee);
            await _db.SaveChangesAsync();

            var subject = "Your Smart Task Tracker Employee Account";

            var body = $@"
                <h2>Welcome to Smart Task Tracker</h2>
                <p>Your employee account has been created by <strong>{org.OrganizationName}</strong>.</p>
                <p><strong>Login Email:</strong> {employee.Email}</p>
                <p><strong>Temporary Password:</strong> {tempPassword}</p>
                <p><strong>Employee Code:</strong> {employee.EmployeeCode}</p>
                <p>Please log in and change your password immediately.</p>
            ";

            await _emailService.SendEmailAsync(employee.Email, subject, body);

            return Ok(new
            {
                message = "Employee created successfully and login details sent to email.",
                employeeCode = empCode,
                loginEmail = employee.Email
            });
        }

        // GET: api/organization/employees
        [HttpGet("employees")]
        public async Task<IActionResult> GetEmployees()
        {
            var org = await GetMyOrg();
            if (org == null)
                return NotFound(new { message = "Organization not found." });

            var employees = await _db.OrganizationEmployees
                .Where(e => e.OrganizationId == org.OrganizationId)
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new
                {
                    e.EmployeeId,
                    e.FullName,
                    e.Email,
                    e.EmployeeCode,
                    e.Department,
                    e.Position,
                    e.IsActive,
                    e.CreatedAt
                })
                .ToListAsync();

            return Ok(employees);
        }

        // PATCH: api/organization/employees/{id}/deactivate
        [HttpPatch("employees/{id}/deactivate")]
        public async Task<IActionResult> DeactivateEmployee(int id)
        {
            var org = await GetMyOrg();
            if (org == null)
                return NotFound(new { message = "Organization not found." });

            var employee = await _db.OrganizationEmployees
                .FirstOrDefaultAsync(e => e.EmployeeId == id &&
                                          e.OrganizationId == org.OrganizationId);

            if (employee == null)
                return NotFound(new { message = "Employee not found." });

            employee.IsActive = false;
            await _db.SaveChangesAsync();

            return Ok(new { message = "Employee deactivated." });
        }
    }
}