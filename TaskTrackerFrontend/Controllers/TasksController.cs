using Microsoft.AspNetCore.Mvc;

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TaskTrackerFrontend.Models;

namespace SmartTaskTracker.Controllers
{
    public class TasksController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private const string API_BASE = "https://localhost:7105/api";

        public TasksController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        // GET: Tasks/Index - All Tasks Page
        [HttpGet]
        public IActionResult Index()
        {
            // Check authentication
            var userId = HttpContext.Session.GetInt32("UserId");
            var authToken = HttpContext.Session.GetString("Token");

            if (userId == null || string.IsNullOrEmpty(authToken))
            {
                return RedirectToAction("Login", "Account");
            }

            // Return the view - tasks will be loaded via JavaScript
            return View();
        }

        // GET: Tasks/Edit/5
        [HttpGet]
        public IActionResult Edit(int? id)
        {
            // Check authentication
            var userId = HttpContext.Session.GetInt32("UserId");
            var authToken = HttpContext.Session.GetString("Token");

            if (userId == null || string.IsNullOrEmpty(authToken))
            {
                return RedirectToAction("Login", "Account");
            }

            if (id == null)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            // Pass the task ID to the view via ViewBag
            ViewBag.TaskId = id;

            // Return the view - the actual data loading will be done via JavaScript
            return View();
        }

        // API endpoint to get task details
        [HttpGet]
        public async Task<IActionResult> GetTask(int id)
        {
            var authToken = HttpContext.Session.GetString("Token");

            if (string.IsNullOrEmpty(authToken))
            {
                return Unauthorized(new { success = false, message = "Not authenticated" });
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

                var response = await client.GetAsync($"{API_BASE}/tasks/{id}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return Content(content, "application/json");
                }
                else
                {
                    return StatusCode((int)response.StatusCode, new { success = false, message = "Failed to fetch task" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // API endpoint to update task
        [HttpPut]
        public async Task<IActionResult> UpdateTask(int id, [FromBody] TaskUpdateModel model)
        {
            var authToken = HttpContext.Session.GetString("Token");

            if (string.IsNullOrEmpty(authToken))
            {
                return Unauthorized(new { success = false, message = "Not authenticated" });
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

                var jsonContent = JsonSerializer.Serialize(model);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await client.PutAsync($"{API_BASE}/tasks/{id}", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return Content(responseContent, "application/json");
                }
                else
                {
                    return StatusCode((int)response.StatusCode, new { success = false, message = "Failed to update task" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // API endpoint to delete task
        [HttpDelete]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var authToken = HttpContext.Session.GetString("Token");

            if (string.IsNullOrEmpty(authToken))
            {
                return Unauthorized(new { success = false, message = "Not authenticated" });
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

                var response = await client.DeleteAsync($"{API_BASE}/tasks/{id}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return Content(content, "application/json");
                }
                else
                {
                    return StatusCode((int)response.StatusCode, new { success = false, message = "Failed to delete task" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
        // GET: Tasks/Calendar
        [HttpGet]
        public IActionResult Calendar()
        {
            // Check authentication
            var userId = HttpContext.Session.GetInt32("UserId");
            var authToken = HttpContext.Session.GetString("Token");

            if (userId == null || string.IsNullOrEmpty(authToken))
            {
                return RedirectToAction("Login", "Account");
            }

            return View();
        }
    }
}