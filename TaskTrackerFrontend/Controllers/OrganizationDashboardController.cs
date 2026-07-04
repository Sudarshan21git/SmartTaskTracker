using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace TaskTrackerFrontend.Controllers
{
    public class OrganizationDashboardController : Controller
    {
        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Organization")
                return RedirectToAction("Login", "Account");

            ViewBag.OrgName = HttpContext.Session.GetString("UserName");
            ViewBag.OrgId = HttpContext.Session.GetInt32("OrganizationId");
            ViewBag.OrgEmail = HttpContext.Session.GetString("UserEmail")
                ?? User.FindFirstValue(ClaimTypes.Email)
                ?? string.Empty;

            // ✅ Check if new employee was just created
            if (TempData["ShowCredentials"]?.ToString() == "true")
            {
                ViewBag.ShowCredentials = true;
                ViewBag.NewEmployeeName = TempData["NewEmployeeName"]?.ToString();
                ViewBag.NewEmployeeEmail = TempData["NewEmployeeEmail"]?.ToString();
                ViewBag.NewEmployeeCode = TempData["NewEmployeeCode"]?.ToString();
                ViewBag.NewEmployeePassword = TempData["NewEmployeePassword"]?.ToString();
            }
            else
            {
                ViewBag.ShowCredentials = false;
            }

            return View();
        }
    }
}