using CNPM_EnglishLearning.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CNPM_EnglishLearning.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Dashboard
        public IActionResult Index()
        {
            return View("~/Areas/Admin/Views/dashboard.html");
        }

        [HttpGet("/Admin/api/Dashboard/Summary")]
        public async Task<IActionResult> Summary()
        {
            try
            {
                var totalStudents = await _context.Users.AsNoTracking().CountAsync(u => u.Role == "student");
                var activeStudents = await _context.Users.AsNoTracking().CountAsync(u => u.Role == "student" && u.IsActive);
                var ongoingClasses = await _context.CourseClasses.AsNoTracking().CountAsync(c => c.Status == "ongoing");
                var upcomingClasses = await _context.CourseClasses.AsNoTracking().CountAsync(c => c.Status == "upcoming");
                var activeEnrollments = await _context.Enrollments.AsNoTracking().CountAsync(e => e.Status == "active");

                var estimatedRevenue = await (
                    from e in _context.Enrollments.AsNoTracking()
                    join cc in _context.CourseClasses.AsNoTracking() on e.ClassId equals cc.Id
                    where e.Status == "active" || e.Status == "completed"
                    select (decimal?)cc.Price
                ).SumAsync() ?? 0m;

                var revenueEnrollmentCount = await _context.Enrollments.AsNoTracking()
                    .CountAsync(e => e.Status == "active" || e.Status == "completed");

                // Monthly enrollments (12 months of current year, or latest year with data)
                var targetYear = DateTime.UtcNow.Year;
                var hasCurrentYear = await _context.Enrollments.AsNoTracking().AnyAsync(e => e.EnrolledAt.Year == targetYear);
                if (!hasCurrentYear)
                {
                    var latestEnrollmentDate = await _context.Enrollments.AsNoTracking()
                        .OrderByDescending(e => e.EnrolledAt)
                        .Select(e => (DateTime?)e.EnrolledAt)
                        .FirstOrDefaultAsync();
                    if (latestEnrollmentDate.HasValue)
                    {
                        targetYear = latestEnrollmentDate.Value.Year;
                    }
                }

                var rawMonthly = await _context.Enrollments.AsNoTracking()
                    .Where(e => e.EnrolledAt.Year == targetYear)
                    .GroupBy(e => e.EnrolledAt.Month)
                    .Select(g => new { Month = g.Key, Count = g.Count() })
                    .ToListAsync();

                var monthlyLookup = rawMonthly.ToDictionary(x => x.Month, x => x.Count);
                var monthlyEnrollments = Enumerable.Range(1, 12)
                    .Select(m => new
                    {
                        month = m,
                        label = $"T{m}",
                        count = monthlyLookup.TryGetValue(m, out var c) ? c : 0
                    })
                    .ToList();

                // Level distribution across Enrollments -> CourseClasses -> Courses
                var rawLevels = await (
                    from e in _context.Enrollments.AsNoTracking()
                    join cc in _context.CourseClasses.AsNoTracking() on e.ClassId equals cc.Id
                    join c in _context.Courses.AsNoTracking() on cc.CourseId equals c.Id
                    group e by c.Level into g
                    select new { Level = g.Key, Count = g.Count() }
                ).ToListAsync();

                var levelCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    ["beginner"] = 0,
                    ["intermediate"] = 0,
                    ["advanced"] = 0
                };

                foreach (var item in rawLevels)
                {
                    var key = (item.Level ?? "beginner").Trim().ToLowerInvariant();
                    if (levelCounts.ContainsKey(key))
                    {
                        levelCounts[key] += item.Count;
                    }
                    else
                    {
                        levelCounts["beginner"] += item.Count;
                    }
                }

                var totalLevelCount = levelCounts.Values.Sum();
                var levelDistribution = new[]
                {
                    new
                    {
                        level = "beginner",
                        label = "Cơ bản (Beginner)",
                        count = levelCounts["beginner"],
                        percentage = totalLevelCount > 0 ? Math.Round(levelCounts["beginner"] * 100.0 / totalLevelCount, 1) : 0.0
                    },
                    new
                    {
                        level = "intermediate",
                        label = "Trung cấp (Intermediate)",
                        count = levelCounts["intermediate"],
                        percentage = totalLevelCount > 0 ? Math.Round(levelCounts["intermediate"] * 100.0 / totalLevelCount, 1) : 0.0
                    },
                    new
                    {
                        level = "advanced",
                        label = "Nâng cao (Advanced)",
                        count = levelCounts["advanced"],
                        percentage = totalLevelCount > 0 ? Math.Round(levelCounts["advanced"] * 100.0 / totalLevelCount, 1) : 0.0
                    }
                };

                // Top 10 recent enrollments
                var recentEnrollments = await (
                    from e in _context.Enrollments.AsNoTracking()
                    join u in _context.Users.AsNoTracking() on e.UserId equals u.Id
                    join cc in _context.CourseClasses.AsNoTracking() on e.ClassId equals cc.Id
                    join c in _context.Courses.AsNoTracking() on cc.CourseId equals c.Id
                    orderby e.EnrolledAt descending, e.Id descending
                    select new
                    {
                        id = e.Id,
                        studentName = u.FullName,
                        studentEmail = u.Email,
                        classId = cc.Id,
                        classCode = cc.ClassCode,
                        className = cc.ClassName,
                        courseTitle = c.Title,
                        price = cc.Price,
                        enrolledAt = e.EnrolledAt,
                        status = e.Status
                    }
                ).Take(10).ToListAsync();

                return Ok(new
                {
                    kpi = new
                    {
                        totalStudents,
                        activeStudents,
                        ongoingClasses,
                        upcomingClasses,
                        activeEnrollments,
                        estimatedRevenue,
                        revenueEnrollmentCount
                    },
                    chartYear = targetYear,
                    monthlyEnrollments,
                    totalLevelEnrollments = totalLevelCount,
                    levelDistribution,
                    recentEnrollments
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Không thể tải dữ liệu tổng quan.", detail = ex.Message });
            }
        }
    }
}
