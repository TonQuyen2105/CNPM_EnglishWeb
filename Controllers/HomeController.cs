using Microsoft.AspNetCore.Mvc;

namespace CNPM_EnglishLearning.Controllers
{
    public class HomeController : Controller
    {
        // GET: / (Trang chủ công khai)
        public IActionResult Index()
        {
            return View("~/index.html");
        }

        // GET: /Home/StyleGuide
        public IActionResult StyleGuide()
        {
            return View("~/styleguide.html");
        }

        // GET: /Home/Error
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("~/Views/Shared/Error.cshtml");
        }
    }
}
