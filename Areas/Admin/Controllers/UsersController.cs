using Microsoft.AspNetCore.Mvc;
using CNPM_EnglishLearning.Models.Entities;
using CNPM_EnglishLearning.Data;

namespace CNPM_EnglishLearning.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context = new();

        // GET: /Admin/Users
        public IActionResult Index(string? search, string? role)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u => u.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                         u.Email.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(role) && role != "all")
            {
                query = query.Where(u => u.Role == role);
            }

            var users = query.ToList();
            return View("~/areas/admin/views/users.html", users);
        }

        // POST: /Admin/Users/Create
        [HttpPost]
        public IActionResult Create(User model)
        {
            if (ModelState.IsValid)
            {
                model.Id = _context.Users.Max(u => u.Id) + 1;
                _context.Users.Add(model);
                return RedirectToAction(nameof(Index));
            }
            return View("~/areas/admin/views/users.html", model);
        }

        // POST: /Admin/Users/ToggleStatus/5
        [HttpPost]
        public IActionResult ToggleStatus(int id)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user != null)
            {
                user.IsActive = !user.IsActive;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
