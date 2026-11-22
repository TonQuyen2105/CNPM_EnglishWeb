using CNPM_EnglishLearning.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CNPM_EnglishLearning.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(ApplicationDbContext context, ILogger<DashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Admin/Dashboard
        public IActionResult Index()
        {
            return View("~/Areas/Admin/Views/dashboard.html");
        }

        [HttpGet("/Admin/api/Dashboard/Summary")]
        public async Task<IActionResult> Summary(CancellationToken cancellationToken)
        {
            try
            {
                await AdminCoreHelpers.SyncExpiredClassesAsync(_context, cancellationToken);

                // 1. KPI học viên: 1 truy vấn GROUP BY thay cho 2 truy vấn COUNT riêng lẻ
                var studentStats = await _context.Users.AsNoTracking()
                    .Where(u => u.Role == "student")
                    .GroupBy(u => u.IsActive)
                    .Select(g => new { IsActive = g.Key, Count = g.Count() })
                    .ToListAsync(cancellationToken);
                var totalStudents = studentStats.Sum(x => x.Count);
                var activeStudents = studentStats.Where(x => x.IsActive).Sum(x => x.Count);

                // 2. KPI lớp học: 1 truy vấn GROUP BY thay cho 2 truy vấn COUNT
                var classStatusCounts = await _context.CourseClasses.AsNoTracking()
                    .Where(c => c.Status == "ongoing" || c.Status == "upcoming")
                    .GroupBy(c => c.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);
                var ongoingClasses = classStatusCounts.GetValueOrDefault("ongoing");
                var upcomingClasses = classStatusCounts.GetValueOrDefault("upcoming");

                // 3. KPI ghi danh & doanh thu ước tính: chỉ tính các ghi danh active/completed thuộc lớp chưa bị hủy (cc.Status != "cancelled")
                var validEnrollmentStats = await (
                    from e in _context.Enrollments.AsNoTracking()
                    join cc in _context.CourseClasses.AsNoTracking() on e.ClassId equals cc.Id
                    where (e.Status == "active" || e.Status == "completed") && cc.Status != "cancelled"
                    group cc.Price by e.Status into g
                    select new
                    {
                        Status = g.Key,
                        Count = g.Count(),
                        Revenue = g.Sum(p => (decimal?)p) ?? 0m
                    }
                ).ToListAsync(cancellationToken);

                var activeEnrollments = validEnrollmentStats
                    .Where(x => x.Status == "active")
                    .Sum(x => x.Count);
                var hasPayments = await _context.Payments.AsNoTracking().AnyAsync(cancellationToken);
                int revenueEnrollmentCount;
                decimal estimatedRevenue;
                if (hasPayments)
                {
                    var completedPayments = await _context.Payments.AsNoTracking()
                        .Where(p => p.Status == "completed")
                        .GroupBy(_ => 1)
                        .Select(g => new { Count = g.Count(), Amount = g.Sum(p => (decimal?)p.Amount) ?? 0m })
                        .FirstOrDefaultAsync(cancellationToken);
                    revenueEnrollmentCount = completedPayments?.Count ?? 0;
                    estimatedRevenue = completedPayments?.Amount ?? 0m;
                }
                else
                {
                    revenueEnrollmentCount = validEnrollmentStats.Sum(x => x.Count);
                    estimatedRevenue = validEnrollmentStats.Sum(x => x.Revenue);
                }

                // 4. Ghi danh theo tháng (12 tháng của năm hiện tại, hoặc năm mới nhất có dữ liệu; loại ghi danh/lớp đã hủy)
                var validEnrollmentsBase =
                    from e in _context.Enrollments.AsNoTracking()
                    join cc in _context.CourseClasses.AsNoTracking() on e.ClassId equals cc.Id
                    where (e.Status == "active" || e.Status == "completed") && cc.Status != "cancelled"
                    select e;

                var targetYear = DateTime.UtcNow.Year;
                var hasCurrentYear = await validEnrollmentsBase
                    .AnyAsync(e => e.EnrolledAt.Year == targetYear, cancellationToken);
                if (!hasCurrentYear)
                {
                    var latestEnrollmentDate = await validEnrollmentsBase
                        .OrderByDescending(e => e.EnrolledAt)
                        .Select(e => (DateTime?)e.EnrolledAt)
                        .FirstOrDefaultAsync(cancellationToken);
                    if (latestEnrollmentDate.HasValue)
                    {
                        targetYear = latestEnrollmentDate.Value.Year;
                    }
                }

                // Lọc theo khoảng thời gian (sargable) thay vì EXTRACT(year) để tận dụng index trên enrolled_at
                var yearStart = new DateTime(targetYear, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var yearEnd = yearStart.AddYears(1);
                var monthlyLookup = await validEnrollmentsBase
                    .Where(e => e.EnrolledAt >= yearStart && e.EnrolledAt < yearEnd)
                    .GroupBy(e => e.EnrolledAt.Month)
                    .Select(g => new { Month = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Month, x => x.Count, cancellationToken);

                var monthlyEnrollments = Enumerable.Range(1, 12)
                    .Select(m => new
                    {
                        month = m,
                        label = $"T{m}",
                        count = monthlyLookup.GetValueOrDefault(m)
                    })
                    .ToList();

                // 5. Phân bổ trình độ: Enrollments -> CourseClasses -> Courses (loại ghi danh/lớp đã hủy)
                var rawLevels = await (
                    from e in _context.Enrollments.AsNoTracking()
                    join cc in _context.CourseClasses.AsNoTracking() on e.ClassId equals cc.Id
                    join c in _context.Courses.AsNoTracking() on cc.CourseId equals c.Id
                    where (e.Status == "active" || e.Status == "completed") && cc.Status != "cancelled"
                    group e by c.Level into g
                    select new { Level = g.Key, Count = g.Count() }
                ).ToListAsync(cancellationToken);

                var levelCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    ["beginner"] = 0,
                    ["intermediate"] = 0,
                    ["advanced"] = 0,
                    ["other"] = 0
                };

                foreach (var item in rawLevels)
                {
                    var key = (item.Level ?? string.Empty).Trim().ToLowerInvariant();
                    if (key is "beginner" or "intermediate" or "advanced")
                    {
                        levelCounts[key] += item.Count;
                    }
                    else
                    {
                        levelCounts["other"] += item.Count;
                    }
                }

                var totalLevelCount = levelCounts.Values.Sum();
                double Percent(int count) => totalLevelCount > 0 ? Math.Round(count * 100.0 / totalLevelCount, 1) : 0.0;

                var levelDistribution = new List<object>
                {
                    new { level = "beginner", label = "Cơ bản (Beginner)", count = levelCounts["beginner"], percentage = Percent(levelCounts["beginner"]) },
                    new { level = "intermediate", label = "Trung cấp (Intermediate)", count = levelCounts["intermediate"], percentage = Percent(levelCounts["intermediate"]) },
                    new { level = "advanced", label = "Nâng cao (Advanced)", count = levelCounts["advanced"], percentage = Percent(levelCounts["advanced"]) }
                };

                if (levelCounts["other"] > 0)
                {
                    levelDistribution.Add(new
                    {
                        level = "other",
                        label = "Trình độ khác",
                        count = levelCounts["other"],
                        percentage = Percent(levelCounts["other"])
                    });
                }

                // 6. Top 10 ghi danh gần nhất (chỉ kéo đúng 10 dòng, đúng các cột cần hiển thị)
                var rawRecentEnrollments = await (
                    from e in _context.Enrollments.AsNoTracking()
                    join u in _context.Users.AsNoTracking() on e.UserId equals u.Id
                    join cc in _context.CourseClasses.AsNoTracking() on e.ClassId equals cc.Id
                    join c in _context.Courses.AsNoTracking() on cc.CourseId equals c.Id
                    orderby e.EnrolledAt descending, e.Id descending
                    select new
                    {
                        id = e.Id,
                        userId = u.Id,
                        studentName = u.FullName,
                        studentEmail = u.Email,
                        classId = cc.Id,
                        classCode = cc.ClassCode,
                        className = cc.ClassName,
                        courseTitle = c.Title,
                        price = cc.Price,
                        classStatus = cc.Status,
                        enrolledAt = e.EnrolledAt,
                        status = e.Status
                    }
                ).Take(10).ToListAsync(cancellationToken);

                var recentIds = rawRecentEnrollments.Select(r => r.id).ToList();
                var recentUserIds = rawRecentEnrollments.Select(r => r.userId).Distinct().ToList();
                var recentClassIds = rawRecentEnrollments.Select(r => r.classId).Distinct().ToList();
                var recentPayments = recentIds.Count == 0
                    ? []
                    : await _context.Payments.AsNoTracking()
                        .Where(p => (p.EnrollmentId != null && recentIds.Contains(p.EnrollmentId.Value))
                                    || (recentUserIds.Contains(p.UserId) && recentClassIds.Contains(p.ClassId)))
                        .OrderByDescending(p => p.Id)
                        .Select(p => new { p.EnrollmentId, p.UserId, p.ClassId, p.Status, p.Amount, p.PaymentMethod })
                        .ToListAsync(cancellationToken);

                var paymentByEnrollment = recentPayments
                    .Where(p => p.EnrollmentId.HasValue)
                    .GroupBy(p => p.EnrollmentId!.Value)
                    .ToDictionary(g => g.Key, g => g.First());
                var paymentByPair = recentPayments
                    .GroupBy(p => (p.UserId, p.ClassId))
                    .ToDictionary(g => g.Key, g => g.First());

                var recentEnrollments = rawRecentEnrollments.Select(r =>
                {
                    if (!paymentByEnrollment.TryGetValue(r.id, out var pay))
                    {
                        paymentByPair.TryGetValue((r.userId, r.classId), out pay);
                    }
                    return new
                    {
                        r.id,
                        r.studentName,
                        r.studentEmail,
                        r.classId,
                        r.classCode,
                        r.className,
                        r.courseTitle,
                        r.price,
                        r.classStatus,
                        r.enrolledAt,
                        r.status,
                        paymentStatus = pay?.Status,
                        paymentAmount = pay?.Amount,
                        paymentMethod = pay?.PaymentMethod
                    };
                }).ToList();

                return Ok(new
                {
                    success = true,
                    kpi = new
                    {
                        totalStudents,
                        activeStudents,
                        ongoingClasses,
                        upcomingClasses,
                        activeEnrollments,
                        estimatedRevenue,
                        revenueEnrollmentCount,
                        revenueSource = hasPayments ? "payments" : "enrollments_estimate"
                    },
                    chartYear = targetYear,
                    monthlyEnrollments,
                    totalLevelEnrollments = totalLevelCount,
                    levelDistribution,
                    recentEnrollments
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể tải dữ liệu tổng quan Dashboard.");
                return this.ApiServerError("Không thể tải dữ liệu tổng quan.");
            }
        }
    }
}
