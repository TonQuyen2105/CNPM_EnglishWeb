using Microsoft.AspNetCore.Mvc;
using CNPM_EnglishLearning.Data;

namespace CNPM_EnglishLearning.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context = new();

        // GET: /Admin/Dashboard
        public IActionResult Index()
        {
            ViewBag.ActiveStudents = _context.Users.Count(u => u.Role == "student" && u.IsActive);
            ViewBag.TotalTeachers = _context.Users.Count(u => u.Role == "teacher");
            ViewBag.OngoingClasses = _context.CourseClasses.Count(c => c.Status == "ongoing");
            ViewBag.EstimatedRevenue = _context.CalculateEstimatedRevenue().ToString("N0") + " ₫";

            return View("~/areas/admin/views/dashboard.html");
        }
    }
}
