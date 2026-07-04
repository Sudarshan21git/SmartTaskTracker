using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using TaskTrackerFrontend.Models;

namespace TaskTrackerFrontend.Controllers
{
    public class OrgEmployeesController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<OrgEmployeesController> _logger;

        public OrgEmployeesController(IHttpClientFactory httpClientFactory,
                                      ILogger<OrgEmployeesController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        // GET: /OrgEmployees/Index
        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Organization")
                return RedirectToAction("Login", "Account");

            ViewBag.OrgId = HttpContext.Session.GetInt32("OrganizationId");
            return View();
        }

        // GET: /OrgEmployees/Create
        public IActionResult Create()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Organization")
                return RedirectToAction("Login", "Account");

            return View(new CreateEmployeeModel());
        }

        // POST: /OrgEmployees/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateEmployeeModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var client = _httpClientFactory.CreateClient("TaskTrackerAPI");
                var token = HttpContext.Session.GetString("Token");

                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue(
                        "Bearer", token);

                var response = await client.PostAsJsonAsync(
                    "api/organization/create-employee", new
                    {
                        firstName = model.FirstName,
                        lastName = model.LastName,
                        email = model.Email,
                        department = model.Department,
                        position = model.Position
                    });

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content
                        .ReadFromJsonAsync<CreateEmployeeResult>(
                            new System.Text.Json.JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                    // ✅ Store credentials in TempData to show popup
                    TempData["ShowCredentials"] = "true";
                    TempData["NewEmployeeName"] = result?.FullName;
                    TempData["NewEmployeeEmail"] = result?.LoginEmail;
                    TempData["NewEmployeeCode"] = result?.EmployeeCode;
                    TempData["NewEmployeePassword"] = result?.TempPassword;

                    return RedirectToAction("Index", "OrganizationDashboard");
                }

                // Handle error
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
                        errorObj?.Message ?? "Failed to create employee.");
                }
                catch
                {
                    ModelState.AddModelError("", "Failed to create employee.");
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Create employee error: {ex.Message}");
                ModelState.AddModelError("", "Something went wrong. Please try again.");
                return View(model);
            }
        }
    }
}