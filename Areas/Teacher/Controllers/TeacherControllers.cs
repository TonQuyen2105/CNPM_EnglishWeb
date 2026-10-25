using Microsoft.AspNetCore.Mvc;
using CNPM_EnglishLearning.Areas.Teacher.ViewModels;
using CNPM_EnglishLearning.Data;

namespace CNPM_EnglishLearning.Areas.Teacher.Controllers
{
    [Area("Teacher")]
    public class DashboardController : Controller
    {
        public IActionResult Index() => View("~/areas/teacher/views/dashboard.html");
    }

    [Area("Teacher")]
    public class ClassesController : Controller
    {
        public IActionResult Index() => View("~/areas/teacher/views/classes.html");
        public IActionResult Detail(int id) => View("~/areas/teacher/views/class-detail.html");
    }

    [Area("Teacher")]
    public class ScheduleController : Controller
    {
        public IActionResult Index() => View("~/areas/teacher/views/schedule.html");
    }

    [Area("Teacher")]
    public class LessonsController : Controller
    {
        public IActionResult Index() => View("~/areas/teacher/views/lessons.html");
    }

    [Area("Teacher")]
    public class QuestionsController : Controller
    {
        public IActionResult Index() => View("~/areas/teacher/views/questions.html");
    }

    [Area("Teacher")]
    public class ExamsController : Controller
    {
        public IActionResult Index() => View("~/areas/teacher/views/exams.html");
    }

    [Area("Teacher")]
    public class GradingController : Controller
    {
        public IActionResult Index() => View("~/areas/teacher/views/grading.html");

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
        public IActionResult Index() => View("~/areas/teacher/views/results.html");
    }

    [Area("Teacher")]
    public class ProfileController : Controller
    {
        public IActionResult Index() => View("~/areas/teacher/views/profile.html");
    }
}
