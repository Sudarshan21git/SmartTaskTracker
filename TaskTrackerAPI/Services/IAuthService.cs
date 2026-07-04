using TaskTrackerAPI.Models; // Change this from Models.DTOs to just Models

namespace TaskTrackerAPI.Services
{
    public interface IAuthService
    {
        Task<AuthResult> RegisterAsync(RegisterRequest request);
        Task<AuthResult> LoginAsync(LoginRequest request);
        Task<bool> UserExistsAsync(string email);

        Task<AuthResult> UpdateProfileAsync(int userId, ProfileUpdateRequest request);
        Task<AuthResult> ChangePasswordAsync(int userId, string role, ChangePasswordRequest request);
        Task<ProfileResponse> GetProfileAsync(int userId);
    }

}