using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TaskTrackerFrontend.Models;
using System.Net.Http.Json;

namespace TaskTrackerFrontend.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(IHttpClientFactory httpClientFactory, ILogger<DashboardController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        // GET: /Dashboard
        public async Task<IActionResult> Index()
        {
            try
            {
                // Get user info from session
                var userId = HttpContext.Session.GetInt32("UserId");
                var userEmail = HttpContext.Session.GetString("UserEmail");
                var userRole = HttpContext.Session.GetString("UserRole");
                var userName = HttpContext.Session.GetString("UserName");

                if (userId == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                // Create dashboard view model
                var dashboardModel = new DashboardViewModel
                {
                    UserId = userId.Value,
                    UserEmail = userEmail ?? "",
                    UserName = userName ?? "",
                    UserRole = userRole ?? "User"
                };

                // Fetch user's tasks from backend
                await LoadDashboardData(dashboardModel);

                return View(dashboardModel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Dashboard error: {ex.Message}");
                TempData["ErrorMessage"] = "Failed to load dashboard data.";
                return View(new DashboardViewModel());
            }
        }

        private async Task LoadDashboardData(DashboardViewModel model)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("TaskTrackerAPI");

                // Add authorization header with JWT token if available
                var token = HttpContext.Session.GetString("Token");
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }

                // Fetch tasks for the current user
                var response = await client.GetAsync($"api/tasks/user/{model.UserId}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<TaskItem>>>();

                    if (result != null && result.Success && result.Data != null)
                    {
                        var tasks = result.Data;

                        // Set recent tasks (last 10 created)
                        model.RecentTasks = tasks
                            .OrderByDescending(t => t.CreatedAt)
                            .Take(10)
                            .ToList();

                        // Set upcoming tasks (not completed, due in future)
                        model.UpcomingTasks = tasks
                            .Where(t => t.Status != 0 && t.DueDate.HasValue && t.DueDate.Value.Date >= DateTime.Today)
                            .OrderBy(t => t.DueDate)
                            .Take(5)
                            .ToList();

                        // Calculate statistics
                        model.TotalTasks = tasks.Count;
                        model.CompletedTasks = tasks.Count(t => t.Status == 0);
                        model.PendingTasks = tasks.Count(t => t.Status == 2);
                        model.InProgressTasks = tasks.Count(t => t.Status == 1);
                        model.OverdueTasks = tasks.Count(t =>
                            t.DueDate.HasValue &&
                            t.DueDate.Value.Date < DateTime.Today &&
                            t.Status != 0);

                        // Priority breakdown
                        model.HighPriorityTasks = tasks.Count(t => t.Priority == 2);
                        model.MediumPriorityTasks = tasks.Count(t => t.Priority == 1);
                        model.LowPriorityTasks = tasks.Count(t => t.Priority == 0);
                    }
                    else
                    {
                        _logger.LogWarning($"No tasks found or API returned failure for user {model.UserId}");
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Failed to fetch tasks: {response.StatusCode} - {errorContent}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"Network error fetching tasks: {ex.Message}");
                model.RecentTasks = new List<TaskItem>();
                model.UpcomingTasks = new List<TaskItem>();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading dashboard data: {ex.Message}");
                model.RecentTasks = new List<TaskItem>();
                model.UpcomingTasks = new List<TaskItem>();
            }
        }

        // POST: /Dashboard/AddTask
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTask(TaskCreateModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please fix validation errors.";
                return RedirectToAction("Index");
            }

            try
            {
                var client = _httpClientFactory.CreateClient("TaskTrackerAPI");

                // Add authorization header
                var token = HttpContext.Session.GetString("Token");
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }

                // Convert to backend TaskItem
                var taskItem = new TaskItem
                {
                    Title = model.Title,
                    Description = model.Description,
                    Priority = model.Priority,
                    Status = model.Status,
                    DueDate = model.DueDate,
                    UserId = HttpContext.Session.GetInt32("UserId") ?? 0,
                    CreatedAt = DateTime.UtcNow
                };

                var response = await client.PostAsJsonAsync("api/tasks", taskItem);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<TaskItem>>();
                    if (result != null && result.Success)
                    {
                        TempData["SuccessMessage"] = result.Message ?? "Task added successfully!";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = result?.Message ?? "Failed to add task.";
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Failed to add task: {response.StatusCode} - {errorContent}");
                    TempData["ErrorMessage"] = "Failed to add task to server.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding task: {ex.Message}");
                TempData["ErrorMessage"] = "Error adding task.";
            }

            return RedirectToAction("Index");
        }

        // POST: /Dashboard/UpdateTaskStatus
        [HttpPost]
        public async Task<IActionResult> UpdateTaskStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("TaskTrackerAPI");

                // Add authorization header
                var token = HttpContext.Session.GetString("Token");
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }

                var response = await client.PatchAsJsonAsync($"api/tasks/{id}/status", request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<TaskItem>>();
                    return Json(new { success = true, message = result?.Message ?? "Status updated" });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Failed to update task status: {response.StatusCode} - {errorContent}");
                    return Json(new { success = false, message = "Failed to update status" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating task status: {ex.Message}");
                return Json(new { success = false, message = "Error updating status" });
            }
        }

        // POST: /Dashboard/DeleteTask
        [HttpPost]
        public async Task<IActionResult> DeleteTask(int id)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("TaskTrackerAPI");

                // Add authorization header
                var token = HttpContext.Session.GetString("Token");
                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }

                var response = await client.DeleteAsync($"api/tasks/{id}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
                    return Json(new
                    {
                        success = true,
                        message = result?.Message ?? "Task deleted successfully"
                    });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Failed to delete task: {response.StatusCode} - {errorContent}");
                    return Json(new { success = false, message = "Failed to delete task" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting task: {ex.Message}");
                return Json(new { success = false, message = "Error deleting task" });
            }
        }
    }
}