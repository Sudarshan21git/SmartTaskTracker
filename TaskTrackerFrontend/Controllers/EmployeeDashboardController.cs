using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace TaskTrackerFrontend.Controllers
{
    public class EmployeeDashboardController : Controller
    {
        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Employee")
                return RedirectToAction("Login", "Account");

            // Check if must change password
            var mustChange = HttpContext.Session.GetString("MustChangePassword");
            ViewBag.MustChangePassword = mustChange == "true";
            ViewBag.EmployeeName = HttpContext.Session.GetString("UserName");
            ViewBag.EmployeeEmail = HttpContext.Session.GetString("Email")
                ?? User.FindFirstValue(ClaimTypes.Email)
                ?? string.Empty;
            ViewBag.Department = HttpContext.Session.GetString("Department");
            ViewBag.EmployeeCode = HttpContext.Session.GetString("EmployeeCode");

            return View();
        }
    }
}