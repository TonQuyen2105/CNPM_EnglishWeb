using Microsoft.AspNetCore.Mvc;
using CNPM_EnglishLearning.Models.Entities;
using CNPM_EnglishLearning.Data;

namespace CNPM_EnglishLearning.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class TeachersController : Controller
    {
        private readonly ApplicationDbContext _context = new();

        public IActionResult Index() => View("~/Areas/Admin/Views/teachers.html", _context.Teachers);

        [HttpPost]
        public IActionResult Verify(int id)
        {
            var teacher = _context.Teachers.FirstOrDefault(t => t.Id == id);
            if (teacher != null) teacher.IsVerified = true;
            return RedirectToAction(nameof(Index));
        }
    }

    [Area("Admin")]
    public class SubjectsController : Controller
    {
        private readonly ApplicationDbContext _context = new();

        public IActionResult Index() => View("~/Areas/Admin/Views/subjects.html", _context.Subjects);
    }

    [Area("Admin")]
    public class CoursesController : Controller
    {
        private readonly ApplicationDbContext _context = new();

        public IActionResult Index() => View("~/Areas/Admin/Views/courses.html", _context.Courses);

        public IActionResult Detail(int id)
        {
            var course = _context.Courses.FirstOrDefault(c => c.Id == id) ?? _context.Courses.First();
            return View("~/Areas/Admin/Views/course-detail.html", course);
        }
    }

    [Area("Admin")]
    public class ReviewsController : Controller
    {
        public IActionResult Index() => View("~/Areas/Admin/Views/reviews.html");
    }

    [Area("Admin")]
    public class ExamsController : Controller
    {
        public IActionResult Index() => View("~/Areas/Admin/Views/exams.html");
    }

    [Area("Admin")]
    public class ReportsController : Controller
    {
        public IActionResult Index() => View("~/Areas/Admin/Views/reports.html");
    }
}
