using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TaskTrackerFrontend.Models;

namespace TaskTrackerFrontend.Controllers
{
    public class SettingsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SettingsController> _logger;
        private const string API_BASE = "https://localhost:7105/api";

        public SettingsController(IHttpClientFactory httpClientFactory, ILogger<SettingsController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        // GET: Settings/Index
        public async Task<IActionResult> Index()
        {
            var token = HttpContext.Session.GetString("Token");
            var userId = HttpContext.Session.GetInt32("UserId");

            _logger.LogInformation("=== SETTINGS PAGE LOAD ===");
            _logger.LogInformation($"Token exists: {!string.IsNullOrEmpty(token)}");
            _logger.LogInformation($"UserId: {userId}");

            if (string.IsNullOrEmpty(token) || !userId.HasValue)
            {
                _logger.LogWarning("Unauthorized access attempt to Settings page");
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var client = _httpClientFactory.CreateClient("TaskTrackerAPI");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                _logger.LogInformation($"Calling API: {API_BASE}/profile");
                var response = await client.GetAsync($"{API_BASE}/profile");
                _logger.LogInformation($"API Response Status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"API Response received");

                    try
                    {
                        // Backend returns { success, profile } not { success, data }
                        var jsonDoc = JsonDocument.Parse(content);
                        if (jsonDoc.RootElement.TryGetProperty("profile", out var profileElement))
                        {
                            var profile = JsonSerializer.Deserialize<UserProfileModel>(profileElement.GetRawText(), new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                            if (profile != null)
                            {
                                _logger.LogInformation("✅ Profile loaded successfully from API");
                                var viewModel = new SettingsViewModel
                                {
                                    UserProfile = profile,
                                    ChangePassword = new ChangePasswordModel()
                                };
                                return View(viewModel);
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "❌ Failed to deserialize API response");
                    }
                }

                // FALLBACK: Load from session if API fails
                _logger.LogWarning("⚠️ Using session data as fallback");
                return View(GetProfileFromSession(userId.Value));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error loading settings");
                TempData["Warning"] = "Unable to load profile from server. Showing cached data.";
                return View(GetProfileFromSession(userId.Value));
            }
        }

        // POST: Settings/UpdateProfile - ONLY accepts profile data
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(UserProfileModel profile)
        {
            var token = HttpContext.Session.GetString("Token");
            var userId = HttpContext.Session.GetInt32("UserId");

            if (string.IsNullOrEmpty(token) || !userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            // Clear ModelState for properties that aren't in UserProfileModel
            ModelState.Remove("ChangePassword.CurrentPassword");
            ModelState.Remove("ChangePassword.NewPassword");
            ModelState.Remove("ChangePassword.ConfirmPassword");

            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Profile validation failed");
                    TempData["Error"] = "Please correct the errors in the form";

                    // Return view with profile data and empty password model
                    var viewModel = new SettingsViewModel
                    {
                        UserProfile = profile,
                        ChangePassword = new ChangePasswordModel()
                    };
                    return View("Index", viewModel);
                }

                _logger.LogInformation($"Updating profile for user {userId}");

                var client = _httpClientFactory.CreateClient("TaskTrackerAPI");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var updateRequest = new
                {
                    firstName = profile.FirstName,
                    lastName = profile.LastName,
                    email = profile.Email
                };

                var jsonContent = JsonSerializer.Serialize(updateRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await client.PutAsync($"{API_BASE}/profile", content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("✅ Profile updated successfully");

                    // Update session data
                    HttpContext.Session.SetString("UserName", $"{profile.FirstName} {profile.LastName}");
                    HttpContext.Session.SetString("UserEmail", profile.Email);

                    TempData["Success"] = "Profile updated successfully";
                    return RedirectToAction("Index");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"❌ Profile update failed. Status: {response.StatusCode}, Error: {errorContent}");

                    // Try to show specific error message from API
                    string errorMessage = "Failed to update profile. Please try again.";
                    try
                    {
                        var errorJson = JsonDocument.Parse(errorContent);
                        if (errorJson.RootElement.TryGetProperty("message", out var msgProperty))
                        {
                            errorMessage = msgProperty.GetString() ?? errorMessage;
                        }
                        else if (errorJson.RootElement.TryGetProperty("title", out var titleProperty))
                        {
                            errorMessage = titleProperty.GetString() ?? errorMessage;
                        }
                    }
                    catch
                    {
                        errorMessage += $" (Status: {response.StatusCode})";
                    }

                    TempData["Error"] = errorMessage;

                    var viewModel = new SettingsViewModel
                    {
                        UserProfile = profile,
                        ChangePassword = new ChangePasswordModel()
                    };
                    return View("Index", viewModel);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error updating profile");
                TempData["Error"] = $"An error occurred: {ex.Message}";

                var viewModel = new SettingsViewModel
                {
                    UserProfile = profile,
                    ChangePassword = new ChangePasswordModel()
                };
                return View("Index", viewModel);
            }
        }

        // POST: Settings/ChangePassword - ONLY accepts password data
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordModel passwordModel)
        {
            var token = HttpContext.Session.GetString("Token");
            var userId = HttpContext.Session.GetInt32("UserId");

            if (string.IsNullOrEmpty(token) || !userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            // Clear ModelState for properties that aren't in ChangePasswordModel
            ModelState.Remove("UserProfile.UserId");
            ModelState.Remove("UserProfile.FirstName");
            ModelState.Remove("UserProfile.LastName");
            ModelState.Remove("UserProfile.Email");
            ModelState.Remove("UserProfile.Role");
            ModelState.Remove("UserProfile.CreatedAt");

            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Password validation failed");
                    TempData["Error"] = "Please correct the errors in the password form";

                    // Get profile data and return with password validation errors
                    var profileData = await GetUserProfileFromAPI() ?? GetProfileFromSession(userId.Value).UserProfile;
                    var viewModel = new SettingsViewModel
                    {
                        UserProfile = profileData,
                        ChangePassword = passwordModel
                    };
                    return View("Index", viewModel);
                }

                _logger.LogInformation($"Changing password for user {userId}");

                var client = _httpClientFactory.CreateClient("TaskTrackerAPI");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var passwordRequest = new
                {
                    currentPassword = passwordModel.CurrentPassword,
                    newPassword = passwordModel.NewPassword,
                    confirmPassword = passwordModel.ConfirmPassword
                };

                var jsonContent = JsonSerializer.Serialize(passwordRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{API_BASE}/profile/change-password", content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("✅ Password changed successfully");
                    TempData["Success"] = "Password changed successfully";
                    return RedirectToAction("Index");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"❌ Failed to change password: {errorContent}");
                    TempData["Error"] = "Failed to change password. Please check your current password.";

                    var profileData = await GetUserProfileFromAPI() ?? GetProfileFromSession(userId.Value).UserProfile;
                    var viewModel = new SettingsViewModel
                    {
                        UserProfile = profileData,
                        ChangePassword = passwordModel
                    };
                    return View("Index", viewModel);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error changing password");
                TempData["Error"] = $"An error occurred: {ex.Message}";

                var profileData = GetProfileFromSession(userId.Value).UserProfile;
                var viewModel = new SettingsViewModel
                {
                    UserProfile = profileData,
                    ChangePassword = passwordModel
                };
                return View("Index", viewModel);
            }
        }

        // Helper method to get user profile from API
        private async Task<UserProfileModel?> GetUserProfileFromAPI()
        {
            try
            {
                var token = HttpContext.Session.GetString("Token");
                if (string.IsNullOrEmpty(token))
                {
                    return null;
                }

                var client = _httpClientFactory.CreateClient("TaskTrackerAPI");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await client.GetAsync($"{API_BASE}/profile");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    // Backend returns { success, profile }
                    var jsonDoc = JsonDocument.Parse(content);
                    if (jsonDoc.RootElement.TryGetProperty("profile", out var profileElement))
                    {
                        var profile = JsonSerializer.Deserialize<UserProfileModel>(profileElement.GetRawText(), new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        return profile;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile from API");
                return null;
            }
        }

        // Helper method to get profile from session (fallback)
        private SettingsViewModel GetProfileFromSession(int userId)
        {
            _logger.LogInformation("Loading profile data from session");

            var userName = HttpContext.Session.GetString("UserName") ?? "";
            var nameParts = userName.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // Try to get CreatedAt from session
            var createdAtString = HttpContext.Session.GetString("CreatedAt");
            DateTime createdAt = default(DateTime); // Don't use DateTime.Now - show as "Not available"

            if (!string.IsNullOrEmpty(createdAtString))
            {
                DateTime.TryParse(createdAtString, out createdAt);
            }

            return new SettingsViewModel
            {
                UserProfile = new UserProfileModel
                {
                    UserId = userId,
                    FirstName = nameParts.Length > 0 ? nameParts[0] : "",
                    LastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "",
                    Email = HttpContext.Session.GetString("UserEmail") ?? "",
                    Role = HttpContext.Session.GetString("UserRole") ?? "User",
                    CreatedAt = createdAt // Will be default if not in session (shown as "Not available")
                },
                ChangePassword = new ChangePasswordModel() // Always initialize empty password model
            };
        }
    }
}