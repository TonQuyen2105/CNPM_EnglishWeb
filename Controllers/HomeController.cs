using Microsoft.AspNetCore.Mvc;

namespace CNPM_EnglishLearning.Controllers
{
    public class HomeController : Controller
    {
        // GET: / hoặc /index.html (Trang đăng nhập công khai)
        [HttpGet("/")]
        [HttpGet("/index.html")]
        [HttpGet("/Account/Login")]
        public IActionResult Index()
        {
            return View("~/index.html");
        }

        // GET: /logout hoặc /Account/Logout (Xử lý đăng xuất)
        [HttpGet("/logout")]
        [HttpGet("/Account/Logout")]
        public IActionResult Logout()
        {
            return Redirect("/");
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
