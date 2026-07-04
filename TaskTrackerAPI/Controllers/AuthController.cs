using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TaskTrackerAPI.Data;
using TaskTrackerAPI.Models;
using TaskTrackerAPI.Services;
using TaskTrackerAPI.Models.DTOs;
namespace TaskTrackerAPI.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IJwtService _jwtService;
        private readonly AppDbContext _db;

        public AuthController(IAuthService authService,
                              IJwtService jwtService,
                              AppDbContext db)
        {
            _authService = authService;
            _jwtService  = jwtService;
            _db          = db;
        }

        // ─────────────────────────────────────────
        // GET: api/auth/test
        // ─────────────────────────────────────────
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { message = "Auth Controller working!", status = "OK" });
        }

        // ─────────────────────────────────────────
        // POST: api/auth/register/user
        // ─────────────────────────────────────────
        [HttpPost("register/user")]
        public async Task<IActionResult> RegisterUser(
            [FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new
                {
                    success = false,
                    message = "Validation failed.",
                    errors
                });
            }

            try
            {
                if (await _authService.UserExistsAsync(request.Email))
                    return BadRequest(new
                    {
                        success = false,
                        message = "Email already exists."
                    });

                var result = await _authService.RegisterAsync(request);

                if (result.Success)
                    return Ok(new { success = true, message = result.Message });

                return BadRequest(new { success = false, message = result.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred during registration."
                });
            }
        }

        // ─────────────────────────────────────────
        // POST: api/auth/register/organization
        // ─────────────────────────────────────────
        [HttpPost("register/organization")]
        public async Task<IActionResult> RegisterOrganization(
            [FromBody] RegisterOrganizationRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    success = false,
                    message = "Validation failed."
                });

            try
            {
                if (await _authService.UserExistsAsync(request.CompanyEmail))
                    return BadRequest(new
                    {
                        success = false,
                        message = "Email already exists."
                    });

                if (await _db.Organizations
                        .AnyAsync(o => o.CompanyEmail == request.CompanyEmail))
                    return BadRequest(new
                    {
                        success = false,
                        message = "Email already exists."
                    });

                var adminUser = new User
                {
                    FirstName    = request.OrganizationName,
                    LastName     = "(Org)",
                    Email        = request.CompanyEmail.Trim().ToLower(),
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    Role         = UserRole.Organization,
                    IsActive     = true
                };

                _db.Users.Add(adminUser);
                await _db.SaveChangesAsync();

                var org = new Organization
                {
                    OrganizationName = request.OrganizationName,
                    CompanyEmail     = request.CompanyEmail.Trim().ToLower(),
                    Phone            = request.Phone,
                    Address          = request.Address,
                    AdminUserId      = adminUser.UserId,
                    IsActive         = true
                };

                _db.Organizations.Add(org);
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Organization registered successfully."
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred during registration."
                });
            }
        }

        // ─────────────────────────────────────────
        // POST: api/auth/login
        // ─────────────────────────────────────────
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid request."
                });

            // ✅ Always normalize email
            var emailNormalized = request.Email.Trim().ToLower();

            // ── STEP 1: Check Users table ──
            var userExists = await _db.Users
                .AnyAsync(u => u.Email.ToLower() == emailNormalized);

            if (userExists)
            {
                // Create normalized request for AuthService
                var normalizedRequest = new LoginRequest
                {
                    Email    = emailNormalized,
                    Password = request.Password
                };

                var result = await _authService.LoginAsync(normalizedRequest);

                if (!result.Success)
                    return Unauthorized(new
                    {
                        success = false,
                        message = result.Message
                    });

                Enum.TryParse<UserRole>(result.Role, true, out var parsedRole);

                var names     = (result.FullName ?? "").Split(' ', 2);
                var firstName = names.Length > 0 ? names[0] : "";
                var lastName  = names.Length > 1 ? names[1] : "";

                var userForToken = new User
                {
                    UserId    = result.UserId,
                    Role      = parsedRole,
                    FirstName = firstName,
                    LastName  = lastName,
                    Email     = emailNormalized
                };

                var token = _jwtService.GenerateToken(userForToken);

                int orgId = 0;
                if (parsedRole == UserRole.Organization)
                {
                    var org = await _db.Organizations
                        .FirstOrDefaultAsync(o => o.AdminUserId == result.UserId);
                    orgId = org?.OrganizationId ?? 0;
                }

                return Ok(new
                {
                    success            = true,
                    message            = result.Message,
                    token              = token,
                    userId             = result.UserId,
                    role               = result.Role,
                    fullName           = result.FullName,
                    organizationId     = orgId,
                    mustChangePassword = false
                });
            }

            // ── STEP 2: Check OrganizationEmployees table ──
            var employee = await _db.OrganizationEmployees
                .Include(e => e.Organization)
                .FirstOrDefaultAsync(e =>
                    e.Email.ToLower() == emailNormalized);

            if (employee != null)
            {
                // ✅ Verify password with try/catch for old hashes
                bool passwordValid;
                try
                {
                    passwordValid = BCrypt.Net.BCrypt.Verify(
                        request.Password, employee.PasswordHash);
                }
                catch
                {
                    passwordValid = false;
                }

                if (!passwordValid)
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Invalid email or password."
                    });

                if (!employee.IsActive)
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Your account has been deactivated."
                    });

                var empToken = _jwtService.GenerateEmployeeToken(employee);

                return Ok(new
                {
                    success            = true,
                    message            = "Login successful.",
                    token              = empToken,
                    userId             = employee.EmployeeId,
                    role               = "Employee",
                    fullName           = employee.FullName,
                    organizationId     = employee.OrganizationId,
                    mustChangePassword = employee.MustChangePassword,
                    employeeCode       = employee.EmployeeCode,
                    department         = employee.Department
                });
            }

            // ── STEP 3: Not found anywhere ──
            return Unauthorized(new
            {
                success = false,
                message = "No account found with this email."
            });
        }

        // ─────────────────────────────────────────
        // POST: api/auth/check-email
        // ─────────────────────────────────────────
        [HttpPost("check-email")]
        public async Task<IActionResult> CheckEmail(
            [FromBody] EmailCheckRequest request)
        {
            var exists = await _authService.UserExistsAsync(request.Email);
            return Ok(new { exists });
        }
    }

 
}