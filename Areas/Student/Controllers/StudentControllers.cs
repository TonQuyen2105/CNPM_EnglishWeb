using Microsoft.AspNetCore.Mvc;
using CNPM_EnglishLearning.Areas.Student.ViewModels;
using CNPM_EnglishLearning.Data;

namespace CNPM_EnglishLearning.Areas.Student.Controllers
{
    [Area("Student")]
    public class DashboardController : Controller
    {
        public IActionResult Index() => View("~/Areas/Student/Views/dashboard.html");
    }

    [Area("Student")]
    public class CoursesController : Controller
    {
        public IActionResult Index() => View("~/Areas/Student/Views/courses.html");
        public IActionResult Detail(int id) => View("~/Areas/Student/Views/course-detail.html");
    }

    [Area("Student")]
    public class MyClassesController : Controller
    {
        public IActionResult Index() => View("~/Areas/Student/Views/my-classes.html");
        public IActionResult Detail(int id) => View("~/Areas/Student/Views/class-detail.html");
    }

    [Area("Student")]
    public class ScheduleController : Controller
    {
        public IActionResult Index() => View("~/Areas/Student/Views/schedule.html");
    }

    [Area("Student")]
    public class LearnController : Controller
    {
        public IActionResult Index(int lessonId = 9) => View("~/Areas/Student/Views/learn.html");
    }

    [Area("Student")]
    public class FlashcardsController : Controller
    {
        private readonly ApplicationDbContext _context = new();

        public IActionResult Index() => View("~/Areas/Student/Views/flashcards.html", _context.Flashcards);
    }

    [Area("Student")]
    public class ExamsController : Controller
    {
        public IActionResult Index() => View("~/Areas/Student/Views/exams.html");
        public IActionResult Take(int id) => View("~/Areas/Student/Views/take-exam.html");
    }

    [Area("Student")]
    public class ResultsController : Controller
    {
        public IActionResult Index() => View("~/Areas/Student/Views/results.html");
        public IActionResult Detail(int id) => View("~/Areas/Student/Views/result-detail.html");
    }

    [Area("Student")]
    public class ProgressController : Controller
    {
        public IActionResult Index() => View("~/Areas/Student/Views/progress.html");
    }

    [Area("Student")]
    public class ReviewsController : Controller
    {
        public IActionResult Index() => View("~/Areas/Student/Views/reviews.html");
    }

    [Area("Student")]
    public class ProfileController : Controller
    {
        public IActionResult Index() => View("~/Areas/Student/Views/profile.html");
    }
}
