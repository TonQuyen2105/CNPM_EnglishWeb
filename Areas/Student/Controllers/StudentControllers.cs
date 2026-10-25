using Microsoft.AspNetCore.Mvc;
using CNPM_EnglishLearning.Areas.Student.ViewModels;
using CNPM_EnglishLearning.Data;

namespace CNPM_EnglishLearning.Areas.Student.Controllers
{
    [Area("Student")]
    public class DashboardController : Controller
    {
        public IActionResult Index() => View("~/areas/student/views/dashboard.html");
    }

    [Area("Student")]
    public class CoursesController : Controller
    {
        public IActionResult Index() => View("~/areas/student/views/courses.html");
        public IActionResult Detail(int id) => View("~/areas/student/views/course-detail.html");
    }

    [Area("Student")]
    public class MyClassesController : Controller
    {
        public IActionResult Index() => View("~/areas/student/views/my-classes.html");
        public IActionResult Detail(int id) => View("~/areas/student/views/class-detail.html");
    }

    [Area("Student")]
    public class ScheduleController : Controller
    {
        public IActionResult Index() => View("~/areas/student/views/schedule.html");
    }

    [Area("Student")]
    public class LearnController : Controller
    {
        public IActionResult Index(int lessonId = 9) => View("~/areas/student/views/learn.html");
    }

    [Area("Student")]
    public class FlashcardsController : Controller
    {
        private readonly ApplicationDbContext _context = new();

        public IActionResult Index() => View("~/areas/student/views/flashcards.html", _context.Flashcards);
    }

    [Area("Student")]
    public class ExamsController : Controller
    {
        public IActionResult Index() => View("~/areas/student/views/exams.html");
        public IActionResult Take(int id) => View("~/areas/student/views/take-exam.html");
    }

    [Area("Student")]
    public class ResultsController : Controller
    {
        public IActionResult Index() => View("~/areas/student/views/results.html");
        public IActionResult Detail(int id) => View("~/areas/student/views/result-detail.html");
    }

    [Area("Student")]
    public class ProgressController : Controller
    {
        public IActionResult Index() => View("~/areas/student/views/progress.html");
    }

    [Area("Student")]
    public class ReviewsController : Controller
    {
        public IActionResult Index() => View("~/areas/student/views/reviews.html");
    }

    [Area("Student")]
    public class ProfileController : Controller
    {
        public IActionResult Index() => View("~/areas/student/views/profile.html");
    }
}
