using System.Globalization;
using System.Text;
using CNPM_EnglishLearning.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CNPM_EnglishLearning.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(ApplicationDbContext context, ILogger<ReportsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Admin/Reports
        public IActionResult Index() => View("~/Areas/Admin/Views/reports.html");

        [HttpGet("/Admin/api/Reports/Summary")]
        public async Task<IActionResult> Summary(int? year, int? quarter, CancellationToken cancellationToken)
        {
            try
            {
                var report = await BuildReportDataAsync(year, quarter, cancellationToken);
                return Ok(new
                {
                    success = true,
                    selectedYear = report.SelectedYear,
                    selectedQuarter = report.SelectedQuarter,
                    availableYears = report.AvailableYears,
                    kpi = report.Kpi,
                    monthlyTrend = report.MonthlyTrend,
                    courseBreakdown = report.CourseBreakdown
                });
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể tải báo cáo vận hành.");
                return this.ApiServerError("Không thể tải báo cáo vận hành.");
            }
        }

        [HttpGet("/Admin/api/Reports/ExportCsv")]
        public async Task<IActionResult> ExportCsv(int? year, int? quarter, CancellationToken cancellationToken)
        {
            try
            {
                var report = await BuildReportDataAsync(year, quarter, cancellationToken);
                var periodLabel = report.SelectedQuarter > 0
                    ? $"Quy-{report.SelectedQuarter}-{report.SelectedYear}"
                    : $"Nam-{report.SelectedYear}";

                var sb = new StringBuilder();
                sb.AppendLine("STT,Khoa hoc,Mon hoc,So lop,Luot ghi danh hop le,Da hoan thanh,Ty le hoan thanh (%),Diem danh gia TB,Doanh thu uoc tinh (VND)");

                for (var i = 0; i < report.CourseBreakdown.Count; i++)
                {
                    var row = report.CourseBreakdown[i];
                    sb.AppendLine(string.Join(",",
                        (i + 1).ToString(CultureInfo.InvariantCulture),
                        EscapeCsv(row.courseTitle),
                        EscapeCsv(row.subjectName),
                        row.classCount.ToString(CultureInfo.InvariantCulture),
                        row.enrollmentCount.ToString(CultureInfo.InvariantCulture),
                        row.completedCount.ToString(CultureInfo.InvariantCulture),
                        row.completionRate.ToString("0.0", CultureInfo.InvariantCulture),
                        row.averageRating?.ToString("0.0", CultureInfo.InvariantCulture) ?? "--",
                        row.revenue.ToString("0", CultureInfo.InvariantCulture)));
                }

                var preamble = Encoding.UTF8.GetPreamble();
                var body = Encoding.UTF8.GetBytes(sb.ToString());
                var bytes = new byte[preamble.Length + body.Length];
                Buffer.BlockCopy(preamble, 0, bytes, 0, preamble.Length);
                Buffer.BlockCopy(body, 0, bytes, preamble.Length, body.Length);

                return File(bytes, "text/csv; charset=utf-8", $"bao-cao-van-hanh-{periodLabel}.csv");
            }
            catch (OperationCanceledException)
            {
                return this.ApiCancelled();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể xuất báo cáo CSV.");
                return this.ApiServerError("Không thể xuất tệp báo cáo.");
            }
        }

        private async Task<ReportSummaryResult> BuildReportDataAsync(int? year, int? quarter, CancellationToken cancellationToken)
        {
            var currentUtcYear = DateTime.UtcNow.Year;
            var enrollmentYears = await _context.Enrollments
                .AsNoTracking()
                .Select(e => e.EnrolledAt.Year)
                .Distinct()
                .ToListAsync(cancellationToken);

            var availableYears = enrollmentYears
                .Append(currentUtcYear)
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();

            var selectedYear = year.HasValue && year.Value >= 2000 && year.Value <= 2100
                ? year.Value
                : (enrollmentYears.Count > 0 ? enrollmentYears.Max() : currentUtcYear);

            var selectedQuarter = quarter.HasValue && quarter.Value >= 1 && quarter.Value <= 4
                ? quarter.Value
                : 0;

            var startMonth = selectedQuarter > 0 ? (selectedQuarter - 1) * 3 + 1 : 1;
            var monthCount = selectedQuarter > 0 ? 3 : 12;

            var rangeStart = new DateTime(selectedYear, startMonth, 1, 0, 0, 0, DateTimeKind.Utc);
            var rangeEnd = rangeStart.AddMonths(monthCount);
            var prevStart = rangeStart.AddMonths(-monthCount);
            var prevEnd = rangeStart;

            var periodEnrollments = await (
                from e in _context.Enrollments.AsNoTracking()
                join cc in _context.CourseClasses.AsNoTracking() on e.ClassId equals cc.Id
                join c in _context.Courses.AsNoTracking() on cc.CourseId equals c.Id
                join s in _context.Subjects.AsNoTracking() on c.SubjectId equals s.Id into subjGroup
                from subj in subjGroup.DefaultIfEmpty()
                where e.EnrolledAt >= rangeStart && e.EnrolledAt < rangeEnd
                select new
                {
                    e.UserId,
                    e.Status,
                    e.EnrolledAt,
                    ClassId = cc.Id,
                    cc.Price,
                    CourseId = c.Id,
                    CourseTitle = c.Title,
                    SubjectName = subj != null ? subj.Name : "Chưa phân môn"
                }
            ).ToListAsync(cancellationToken);

            var validEnrollments = periodEnrollments
                .Where(x => x.Status == "active" || x.Status == "completed")
                .ToList();

            var periodRevenue = validEnrollments.Sum(x => x.Price);
            var completedCount = validEnrollments.Count(x => x.Status == "completed");
            var completionRate = validEnrollments.Count > 0
                ? Math.Round(completedCount * 100.0 / validEnrollments.Count, 1)
                : 0.0;

            var previousRevenue = await (
                from e in _context.Enrollments.AsNoTracking()
                join cc in _context.CourseClasses.AsNoTracking() on e.ClassId equals cc.Id
                where e.EnrolledAt >= prevStart && e.EnrolledAt < prevEnd
                      && (e.Status == "active" || e.Status == "completed")
                select (decimal?)cc.Price
            ).SumAsync(cancellationToken) ?? 0m;

            double? revenueGrowthPercent = previousRevenue > 0
                ? Math.Round((double)((periodRevenue - previousRevenue) / previousRevenue) * 100.0, 1)
                : null;

            // Tỷ lệ học viên tái đăng ký (>= 2 lượt ghi danh hợp lệ từ trước đến cuối kỳ đang xem)
            var studentClassCounts = await _context.Enrollments
                .AsNoTracking()
                .Where(e => e.EnrolledAt < rangeEnd && (e.Status == "active" || e.Status == "completed"))
                .GroupBy(e => e.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var totalEnrolledStudents = studentClassCounts.Count;
            var returningStudents = studentClassCounts.Count(x => x.Count >= 2);
            var retentionRate = totalEnrolledStudents > 0
                ? Math.Round(returningStudents * 100.0 / totalEnrolledStudents, 1)
                : 0.0;

            var activeStudents = await _context.Users
                .AsNoTracking()
                .CountAsync(u => u.Role == "student" && u.IsActive, cancellationToken);

            var openClasses = await _context.CourseClasses
                .AsNoTracking()
                .CountAsync(cc => cc.Status == "ongoing" || cc.Status == "upcoming", cancellationToken);

            var reviewStats = await _context.CourseReviews
                .AsNoTracking()
                .GroupBy(r => r.CourseId)
                .Select(g => new
                {
                    CourseId = g.Key,
                    Count = g.Count(),
                    Avg = g.Average(r => (double)r.Rating)
                })
                .ToDictionaryAsync(x => x.CourseId, cancellationToken);

            var totalReviewCount = reviewStats.Values.Sum(x => x.Count);
            var overallRating = totalReviewCount > 0
                ? Math.Round(reviewStats.Values.Sum(x => x.Avg * x.Count) / totalReviewCount, 1)
                : 0.0;

            var monthlyTrend = Enumerable.Range(startMonth, monthCount)
                .Select(m =>
                {
                    var monthRows = validEnrollments.Where(x => x.EnrolledAt.Month == m).ToList();
                    return new
                    {
                        month = m,
                        label = $"T{m}",
                        enrollments = monthRows.Count,
                        completed = monthRows.Count(x => x.Status == "completed"),
                        revenue = monthRows.Sum(x => x.Price)
                    };
                })
                .ToList<object>();

            var allCourses = await (
                from c in _context.Courses.AsNoTracking()
                join s in _context.Subjects.AsNoTracking() on c.SubjectId equals s.Id into subjGroup
                from subj in subjGroup.DefaultIfEmpty()
                select new
                {
                    c.Id,
                    c.Title,
                    SubjectName = subj != null ? subj.Name : "Chưa phân môn",
                    ClassCount = _context.CourseClasses.Count(cc => cc.CourseId == c.Id && cc.Status != "cancelled")
                }
            ).ToListAsync(cancellationToken);

            var enrollmentsByCourse = validEnrollments
                .GroupBy(x => x.CourseId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var courseBreakdown = allCourses
                .Select(c =>
                {
                    enrollmentsByCourse.TryGetValue(c.Id, out var list);
                    var count = list?.Count ?? 0;
                    var completed = list?.Count(x => x.Status == "completed") ?? 0;
                    var revenue = list?.Sum(x => x.Price) ?? 0m;
                    var rate = count > 0 ? Math.Round(completed * 100.0 / count, 1) : 0.0;
                    double? avgRating = reviewStats.TryGetValue(c.Id, out var rInfo)
                        ? Math.Round(rInfo.Avg, 1)
                        : null;

                    return new CourseBreakdownRow(
                        c.Id,
                        c.Title,
                        c.SubjectName,
                        c.ClassCount,
                        count,
                        completed,
                        rate,
                        avgRating,
                        revenue);
                })
                .OrderByDescending(x => x.revenue)
                .ThenByDescending(x => x.enrollmentCount)
                .ThenBy(x => x.courseTitle)
                .ToList();

            var kpi = new
            {
                periodRevenue,
                previousRevenue,
                revenueGrowthPercent,
                validEnrollmentCount = validEnrollments.Count,
                completedCount,
                completionRate,
                retentionRate,
                returningStudents,
                totalEnrolledStudents,
                activeStudents,
                openClasses,
                overallRating,
                totalReviewCount
            };

            return new ReportSummaryResult(selectedYear, selectedQuarter, availableYears, kpi, monthlyTrend, courseBreakdown);
        }

        private static string EscapeCsv(string? value)
        {
            var raw = value ?? string.Empty;
            if (raw.Length > 0 && (raw[0] is '=' or '+' or '-' or '@' or '\t' or '\r'))
            {
                raw = "'" + raw;
            }
            return $"\"{raw.Replace("\"", "\"\"")}\"";
        }

        private sealed record CourseBreakdownRow(
            int courseId,
            string courseTitle,
            string subjectName,
            int classCount,
            int enrollmentCount,
            int completedCount,
            double completionRate,
            double? averageRating,
            decimal revenue);

        private sealed record ReportSummaryResult(
            int SelectedYear,
            int SelectedQuarter,
            List<int> AvailableYears,
            object Kpi,
            List<object> MonthlyTrend,
            List<CourseBreakdownRow> CourseBreakdown);
    }
}
