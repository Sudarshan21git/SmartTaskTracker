using Microsoft.EntityFrameworkCore;
using TaskTrackerAPI.Data;
using TaskTrackerAPI.Models;

namespace TaskTrackerAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;

        public AuthService(AppDbContext context, IJwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        public async Task<AuthResult> RegisterAsync(RegisterRequest request)
        {
            try
            {
                if (await UserExistsAsync(request.Email))
                    return new AuthResult
                    {
                        Success = false,
                        Message = "Email already exists."
                    };

                var user = new User
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    Role = UserRole.User,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return new AuthResult
                {
                    Success = true,
                    Message = "Registration successful.",
                    UserId = user.UserId,
                    FullName = user.FullName,
                    Role = user.Role.ToString()
                };
            }
            catch (Exception ex)
            {
                return new AuthResult
                {
                    Success = false,
                    Message = $"Registration failed: {ex.Message}"
                };
            }
        }

        public async Task<AuthResult> LoginAsync(LoginRequest request)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (user == null)
                    return new AuthResult
                    {
                        Success = false,
                        Message = "No account found with this email."
                    };

                bool passwordValid;
                try
                {
                    passwordValid = BCrypt.Net.BCrypt.Verify(
                        request.Password, user.PasswordHash);
                }
                catch
                {
                    passwordValid = false;
                }

                if (!passwordValid)
                    return new AuthResult
                    {
                        Success = false,
                        Message = "Invalid email or password."
                    };

                if (!user.IsActive)
                    return new AuthResult
                    {
                        Success = false,
                        Message = "Your account has been deactivated."
                    };

                return new AuthResult
                {
                    Success = true,
                    Message = "Login successful.",
                    UserId = user.UserId,
                    FullName = user.FullName,
                    Role = user.Role.ToString()
                };
            }
            catch (Exception ex)
            {
                return new AuthResult
                {
                    Success = false,
                    Message = $"Login failed: {ex.Message}"
                };
            }
        }

        public async Task<bool> UserExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<ProfileResponse> GetProfileAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
                return null!;

            return new ProfileResponse
            {
                UserId = user.UserId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString(),
                CreatedAt = user.CreatedAt
            };
        }

        public async Task<AuthResult> UpdateProfileAsync(int userId,
            ProfileUpdateRequest request)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                    return new AuthResult
                    {
                        Success = false,
                        Message = "User not found."
                    };

                if (user.Email != request.Email)
                {
                    var existing = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email == request.Email
                                               && u.UserId != userId);
                    if (existing != null)
                        return new AuthResult
                        {
                            Success = false,
                            Message = "Email is already taken by another user."
                        };
                }

                user.FirstName = request.FirstName;
                user.LastName = request.LastName;
                user.Email = request.Email;

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                return new AuthResult
                {
                    Success = true,
                    Message = "Profile updated successfully.",
                    UserId = user.UserId,
                    FullName = user.FullName,
                    Role = user.Role.ToString()
                };
            }
            catch (Exception ex)
            {
                return new AuthResult
                {
                    Success = false,
                    Message = $"Error updating profile: {ex.Message}"
                };
            }
        }

        public async Task<AuthResult> ChangePasswordAsync(int userId,
            string role,
            ChangePasswordRequest request)
        {
            try
            {
                bool isEmployee = string.Equals(role, "Employee", StringComparison.OrdinalIgnoreCase);

                if (isEmployee)
                {
                    var employee = await _context.OrganizationEmployees.FindAsync(userId);

                    if (employee == null)
                        return new AuthResult
                        {
                            Success = false,
                            Message = "User not found."
                        };

                    bool currentValid;
                    try
                    {
                        currentValid = BCrypt.Net.BCrypt.Verify(
                            request.CurrentPassword, employee.PasswordHash);
                    }
                    catch
                    {
                        currentValid = false;
                    }

                    if (!currentValid)
                        return new AuthResult
                        {
                            Success = false,
                            Message = "Current password is incorrect."
                        };

                    employee.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                    employee.MustChangePassword = false;
                    _context.OrganizationEmployees.Update(employee);

                    await _context.SaveChangesAsync();

                    return new AuthResult
                    {
                        Success = true,
                        Message = "Password changed successfully.",
                        UserId = employee.EmployeeId,
                        FullName = employee.FullName,
                        Role = "Employee"
                    };
                }

                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                    return new AuthResult
                    {
                        Success = false,
                        Message = "User not found."
                    };

                bool userCurrentValid;
                try
                {
                    userCurrentValid = BCrypt.Net.BCrypt.Verify(
                        request.CurrentPassword, user.PasswordHash);
                }
                catch
                {
                    userCurrentValid = false;
                }

                if (!userCurrentValid)
                    return new AuthResult
                    {
                        Success = false,
                        Message = "Current password is incorrect."
                    };

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                return new AuthResult
                {
                    Success = true,
                    Message = "Password changed successfully.",
                    UserId = user.UserId,
                    FullName = user.FullName,
                    Role = user.Role.ToString()
                };
            }
            catch (Exception ex)
            {
                return new AuthResult
                {
                    Success = false,
                    Message = $"Error changing password: {ex.Message}"
                };
            }
        }
    }
}