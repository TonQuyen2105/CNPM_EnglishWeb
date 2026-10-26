using Microsoft.AspNetCore.Mvc;
using CNPM_EnglishLearning.Data;
using CNPM_EnglishLearning.Models.Entities;

namespace CNPM_EnglishLearning.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context = new();

        // GET: /Account/Login hoặc /login.html hoặc /login
        [HttpGet("/Account/Login")]
        [HttpGet("/login")]
        [HttpGet("/login.html")]
        public IActionResult Login()
        {
            return View("~/login.html");
        }

        // POST: /Account/Login
        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            var user = _context.Users.FirstOrDefault(u => 
                u.Email.Equals(email, StringComparison.OrdinalIgnoreCase) && 
                u.Password == password && 
                u.IsActive);

            if (user == null)
            {
                ViewBag.Error = "Email hoặc mật khẩu không chính xác.";
                return View("~/login.html");
            }

            // Chuyển hướng theo vai trò (Role-based redirect)
            return user.Role.ToLower() switch
            {
                "admin" => RedirectToAction("Index", "Dashboard", new { area = "Admin" }),
                "teacher" => RedirectToAction("Index", "Dashboard", new { area = "Teacher" }),
                _ => RedirectToAction("Index", "Dashboard", new { area = "Student" })
            };
        }

        // GET: /Account/Logout
        public IActionResult Logout()
        {
            return Redirect("/");
        }
    }
}
