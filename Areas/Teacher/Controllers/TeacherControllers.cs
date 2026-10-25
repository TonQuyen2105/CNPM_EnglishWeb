using Microsoft.AspNetCore.Mvc;
using CNPM_EnglishLearning.Areas.Teacher.ViewModels;
using CNPM_EnglishLearning.Data;

namespace CNPM_EnglishLearning.Areas.Teacher.Controllers
{
    [Area("Teacher")]
    public class DashboardController : Controller
    {
        public IActionResult Index() => View("~/Areas/Teacher/Views/dashboard.html");
    }

    [Area("Teacher")]
    public class ClassesController : Controller
    {
        public IActionResult Index() => View("~/Areas/Teacher/Views/classes.html");
        public IActionResult Detail(int id) => View("~/Areas/Teacher/Views/class-detail.html");
    }

    [Area("Teacher")]
    public class ScheduleController : Controller
    {
        public IActionResult Index() => View("~/Areas/Teacher/Views/schedule.html");
    }

    [Area("Teacher")]
    public class LessonsController : Controller
    {
        public IActionResult Index() => View("~/Areas/Teacher/Views/lessons.html");
    }

    [Area("Teacher")]
    public class QuestionsController : Controller
    {
        public IActionResult Index() => View("~/Areas/Teacher/Views/questions.html");
    }

    [Area("Teacher")]
    public class ExamsController : Controller
    {
        public IActionResult Index() => View("~/Areas/Teacher/Views/exams.html");
    }

    [Area("Teacher")]
    public class GradingController : Controller
    {
        public IActionResult Index() => View("~/Areas/Teacher/Views/grading.html");

        [HttpPost]
        public IActionResult SubmitGrade(GradingViewModel model)
        {
            // Xử lý lưu điểm bài thi tự luận
            return RedirectToAction(nameof(Index));
        }
    }

    [Area("Teacher")]
    public class ResultsController : Controller
    {
        public IActionResult Index() => View("~/Areas/Teacher/Views/results.html");
    }

    [Area("Teacher")]
    public class ProfileController : Controller
    {
        public IActionResult Index() => View("~/Areas/Teacher/Views/profile.html");
    }
}
