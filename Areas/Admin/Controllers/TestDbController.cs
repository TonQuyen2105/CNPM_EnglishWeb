using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CNPM_EnglishLearning.Data;

namespace CNPM_EnglishLearning.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("[area]/[controller]")]
    public class TestDbController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TestDbController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var isConnected = await _context.Database.CanConnectAsync();

            var userCount = 0;
            var courseCount = 0;

            if (isConnected)
            {
                userCount = await _context.Users.CountAsync();
                courseCount = await _context.Courses.CountAsync();
            }

            return Ok(new
            {
                connected = isConnected,
                userCount,
                courseCount
            });
        }
    }
}
