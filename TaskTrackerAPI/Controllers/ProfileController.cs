using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskTrackerAPI.Models;
using TaskTrackerAPI.Services;

namespace TaskTrackerAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IJwtService _jwtService;

        public ProfileController(IAuthService authService, IJwtService jwtService)
        {
            _authService = authService;
            _jwtService = jwtService;
        }

        // GET: api/profile
        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                // Get userId from JWT token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Invalid token"
                    });
                }

                var profile = await _authService.GetProfileAsync(userId);

                if (profile == null)
                    return NotFound(new
                    {
                        success = false,
                        message = "Profile not found"
                    });

                return Ok(new
                {
                    success = true,
                    profile = profile
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error"
                });
            }
        }

        // PUT: api/profile
        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] ProfileUpdateRequest request)
        {
            try
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
                        message = "Validation failed",
                        errors = errors
                    });
                }

                // Get userId from JWT token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Invalid token"
                    });
                }

                var result = await _authService.UpdateProfileAsync(userId, request);

                if (result.Success)
                {
                    // Generate new token with updated information
                    var userForToken = new User
                    {
                        UserId = result.UserId,
                        FirstName = request.FirstName,
                        LastName = request.LastName,
                        Email = request.Email,
                        Role = Enum.Parse<UserRole>(result.Role)
                    };

                    var token = _jwtService.GenerateToken(userForToken);

                    return Ok(new
                    {
                        success = true,
                        message = result.Message,
                        token = token,
                        fullName = result.FullName
                    });
                }

                return BadRequest(new
                {
                    success = false,
                    message = result.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while updating profile"
                });
            }
        }

        // POST: api/profile/change-password
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
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
                        message = "Validation failed",
                        errors = errors
                    });
                }

                // Get userId from JWT token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Invalid token"
                    });
                }

                var role = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

                var result = await _authService.ChangePasswordAsync(userId, role, request);

                if (result.Success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = result.Message
                    });
                }

                return BadRequest(new
                {
                    success = false,
                    message = result.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while changing password"
                });
            }
        }
    }
}