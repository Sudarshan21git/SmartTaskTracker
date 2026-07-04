using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Security.Claims;
using TaskTrackerFrontend.Models;

namespace TaskTrackerFrontend.Controllers
{
    public class AccountController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IHttpClientFactory httpClientFactory,
                                 ILogger<AccountController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        // ─────────────────────────────────────────
        // GET: /Account/Register
        // ─────────────────────────────────────────
        public IActionResult Register()
        {
            return View(new RegisterModel());
        }

        // ─────────────────────────────────────────
        // POST: /Account/Register
        // ─────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            // Validate based on account type
            if (model.AccountType == "User")
            {
                if (string.IsNullOrWhiteSpace(model.FirstName))
                    ModelState.AddModelError("FirstName", "First name is required");
                else if (model.FirstName.Length < 3)
                    ModelState.AddModelError("FirstName", "First name must be at least 3 characters");

                if (string.IsNullOrWhiteSpace(model.LastName))
                    ModelState.AddModelError("LastName", "Last name is required");
                else if (model.LastName.Length < 3)
                    ModelState.AddModelError("LastName", "Last name must be at least 3 characters");
            }

            if (model.AccountType == "Organization")
            {
                if (string.IsNullOrWhiteSpace(model.OrganizationName))
                    ModelState.AddModelError("OrganizationName", "Company name is required");
            }

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var client = _httpClientFactory.CreateClient("TaskTrackerAPI");
                HttpResponseMessage response;

                if (model.AccountType == "Organization")
                {
                    var orgData = new
                    {
                        organizationName = model.OrganizationName,
                        companyEmail = model.Email,
                        password = model.Password,
                        phone = model.Phone,
                        address = model.Address
                    };
                    response = await client.PostAsJsonAsync(
                        "api/auth/register/organization", orgData);
                }
                else
                {
                    var userData = new
                    {
                        firstName = model.FirstName,
                        lastName = model.LastName,
                        email = model.Email,
                        password = model.Password
                    };
                    response = await client.PostAsJsonAsync(
                        "api/auth/register/user", userData);
                }

                if (response.IsSuccessStatusCode)
                {
                    ViewBag.ShowSuccess = true;
                    ViewBag.SuccessMessage = model.AccountType == "Organization"
                        ? "Organization registered successfully!"
                        : "Account created successfully!";
                    return View(model);
                }

                // ✅ FIXED — parse JSON to show clean message
                var errorJson = await response.Content.ReadAsStringAsync();
                try
                {
                    var errorObj = System.Text.Json.JsonSerializer.Deserialize<AuthResult>(
                        errorJson,
                        new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                    ModelState.AddModelError("", errorObj?.Message ?? "Registration failed.");
                }
                catch
                {
                    ModelState.AddModelError("", "Registration failed. Please try again.");
                }
                return View(model);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"API error: {ex.Message}");
                ModelState.AddModelError("", "Unable to connect to server.");
                return View(model);
            }
        }

        // ─────────────────────────────────────────
        // GET: /Account/Login
        // ─────────────────────────────────────────
        public IActionResult Login()
        {
            if (TempData["RegistrationSuccess"] != null)
                ViewBag.SuccessMessage = TempData["RegistrationSuccess"];

            return View(new LoginModel());
        }

        // ─────────────────────────────────────────
        // POST: /Account/Login
        // ─────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var client = _httpClientFactory.CreateClient("TaskTrackerAPI");

                var response = await client.PostAsJsonAsync("api/auth/login", new
                {
                    Email = model.Email,
                    Password = model.Password
                });

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content
                                       .ReadFromJsonAsync<AuthResult>();

                    if (result != null && result.Success)
                    {
                        // ── Store Claims ──
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name,  result.FullName ?? model.Email),
                            new Claim(ClaimTypes.Email, model.Email),
                            new Claim(ClaimTypes.Role,  result.Role),
                            new Claim("UserId",         result.UserId.ToString())
                        };

                        var identity = new ClaimsIdentity(claims, "CookieAuth");
                        var principal = new ClaimsPrincipal(identity);
                        await HttpContext.SignInAsync("CookieAuth", principal);

                        // ── Store Common Session ──
                        HttpContext.Session.SetInt32("UserId", result.UserId);
                        HttpContext.Session.SetString("UserEmail", model.Email);
                        HttpContext.Session.SetString("UserRole", result.Role);
                        HttpContext.Session.SetString("UserName", result.FullName ?? model.Email);
                        HttpContext.Session.SetString("Token", result.Token);

                        // ── Store Extra Session by Role ──
                        if (result.Role == "Organization")
                        {
                            HttpContext.Session.SetInt32("OrganizationId",
                                result.OrganizationId);
                        }

                        if (result.Role == "Employee")
                        {
                            HttpContext.Session.SetInt32("OrganizationId",
                                result.OrganizationId);
                            HttpContext.Session.SetString("EmployeeCode",
                                result.EmployeeCode ?? "");
                            HttpContext.Session.SetString("Department",
                                result.Department ?? "");
                            HttpContext.Session.SetString("MustChangePassword",
                                result.MustChangePassword ? "true" : "false");
                        }

                        _logger.LogInformation(
                            $"Login: {model.Email} | Role: {result.Role}");

                        TempData["SuccessMessage"] = "Login successful!";

                        // ── Redirect by Role ──
                        if (result.Role == "User")
                        {
                            return RedirectToAction("Index", "Dashboard");
                        }
                        else if (result.Role == "Organization")
                        {
                            return RedirectToAction("Index", "OrganizationDashboard");
                        }
                        else if (result.Role == "Employee")
                        {
                            if (result.MustChangePassword)
                            {
                                TempData["WarningMessage"] =
                                    "You must change your password before continuing.";
                                return RedirectToAction("ChangePassword", "Account");
                            }
                            return RedirectToAction("Index", "EmployeeDashboard");
                        }

                        return RedirectToAction("Index", "Dashboard");
                    }

                    // ✅ Parse error message cleanly
                    ModelState.AddModelError("",
                        result?.Message ?? "Invalid email or password.");
                    return View(model);
                }

                // ✅ Handle non-success response cleanly
                var errorJson = await response.Content.ReadAsStringAsync();
                try
                {
                    var errorObj = System.Text.Json.JsonSerializer.Deserialize<AuthResult>(
                        errorJson,
                        new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                    ModelState.AddModelError("",
                        errorObj?.Message ?? "Invalid login attempt.");
                }
                catch
                {
                    ModelState.AddModelError("", "Invalid login attempt. Please try again.");
                }
                return View(model);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"API connection error: {ex.Message}");
                ModelState.AddModelError("",
                    "Unable to connect to server. Please try again later.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error: {ex.Message}");
                ModelState.AddModelError("",
                    "An unexpected error occurred. Please try again.");
                return View(model);
            }
        }

        // ─────────────────────────────────────────
        // GET: /Account/ChangePassword
        // ─────────────────────────────────────────
        [HttpGet]
        public IActionResult ChangePassword()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(role))
                return RedirectToAction("Login");

            return View(new ChangePasswordModel());
        }

        // ─────────────────────────────────────────
        // POST: /Account/ChangePassword
        // ─────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var client = _httpClientFactory.CreateClient("TaskTrackerAPI");

                var token = HttpContext.Session.GetString("Token");
                if (string.IsNullOrWhiteSpace(token))
                {
                    TempData["WarningMessage"] = "Your session expired. Please log in again.";
                    return RedirectToAction("Login");
                }

                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await client.PostAsJsonAsync(
                    "api/profile/change-password", new
                    {
                        CurrentPassword = model.CurrentPassword,
                        NewPassword = model.NewPassword,
                        ConfirmPassword = model.ConfirmPassword
                    });

                if (response.IsSuccessStatusCode)
                {
                    // Clear the must change password flag
                    HttpContext.Session.SetString("MustChangePassword", "false");

                    TempData["SuccessMessage"] = "Password changed successfully!";
                    return RedirectToAction("Index", "EmployeeDashboard");
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    TempData["WarningMessage"] = "Your session expired. Please log in again.";
                    return RedirectToAction("Login");
                }

                var errorJson = await response.Content.ReadAsStringAsync();
                try
                {
                    var errorObj = System.Text.Json.JsonSerializer.Deserialize<AuthResult>(
                        errorJson,
                        new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                    ModelState.AddModelError("",
                        errorObj?.Message ?? "Failed to change password.");
                }
                catch
                {
                    ModelState.AddModelError("",
                        "Failed to change password. Check your current password.");
                }
                return View(model);
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Something went wrong. Please try again.");
                return View(model);
            }
        }

        // ─────────────────────────────────────────
        // GET: /Account/Logout
        // ─────────────────────────────────────────
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CookieAuth");
            HttpContext.Session.Clear();
            Response.Cookies.Delete("AuthToken");
            TempData["SuccessMessage"] = "You have been logged out successfully.";
            return RedirectToAction("Login", "Account");
        }

        // ─────────────────────────────────────────
        // GET: /Account/AccessDenied
        // ─────────────────────────────────────────
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}