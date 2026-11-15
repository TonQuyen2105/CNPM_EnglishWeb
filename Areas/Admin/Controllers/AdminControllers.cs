using Microsoft.AspNetCore.Mvc;

namespace CNPM_EnglishLearning.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ReportsController : Controller
    {
        public IActionResult Index() => View("~/Areas/Admin/Views/reports.html");
    }
}
