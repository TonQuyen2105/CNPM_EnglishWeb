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
                    courseBreakdown = report.CourseBreakdown,
                    subjectBreakdown = report.SubjectBreakdown,
                    teacherBreakdown = report.TeacherBreakdown
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
                        AdminCoreHelpers.EscapeCsv(row.courseTitle),
                        AdminCoreHelpers.EscapeCsv(row.subjectName),
                        row.classCount.ToString(CultureInfo.InvariantCulture),
                        row.enrollmentCount.ToString(CultureInfo.InvariantCulture),
                        row.completedCount.ToString(CultureInfo.InvariantCulture),
                        row.completionRate.ToString("0.0", CultureInfo.InvariantCulture),
                        row.averageRating?.ToString("0.0", CultureInfo.InvariantCulture) ?? "--",
                        row.revenue.ToString("0", CultureInfo.InvariantCulture)));
                }

                sb.AppendLine();
                sb.AppendLine("THONG KE DOANH THU THEO MON HOC");
                sb.AppendLine("STT,Mon hoc,So khoa,So lop,Luot ghi danh,Da hoan thanh,Ty le hoan thanh (%),Ty trong doanh thu (%),Doanh thu uoc tinh (VND)");
                for (var i = 0; i < report.SubjectBreakdown.Count; i++)
                {
                    var s = report.SubjectBreakdown[i];
                    sb.AppendLine(string.Join(",",
                        (i + 1).ToString(CultureInfo.InvariantCulture),
                        AdminCoreHelpers.EscapeCsv(s.subjectName),
                        s.courseCount.ToString(CultureInfo.InvariantCulture),
                        s.classCount.ToString(CultureInfo.InvariantCulture),
                        s.enrollmentCount.ToString(CultureInfo.InvariantCulture),
                        s.completedCount.ToString(CultureInfo.InvariantCulture),
                        s.completionRate.ToString("0.0", CultureInfo.InvariantCulture),
                        s.revenueSharePercent.ToString("0.0", CultureInfo.InvariantCulture),
                        s.revenue.ToString("0", CultureInfo.InvariantCulture)));
                }

                sb.AppendLine();
                sb.AppendLine("THONG KE DOANH THU PHAN BO THEO GIANG VIEN");
                sb.AppendLine("STT,Giang vien,Trinh do,Lop chinh / Tong lop,Luot hoc vien,Da hoan thanh,Ty le hoan thanh (%),Ty trong doanh thu (%),Doanh thu phan bo (VND)");
                for (var i = 0; i < report.TeacherBreakdown.Count; i++)
                {
                    var t = report.TeacherBreakdown[i];
                    sb.AppendLine(string.Join(",",
                        (i + 1).ToString(CultureInfo.InvariantCulture),
                        AdminCoreHelpers.EscapeCsv(t.teacherName),
                        AdminCoreHelpers.EscapeCsv(t.qualification ?? "--"),
                        AdminCoreHelpers.EscapeCsv($"{t.mainClassCount}/{t.assignedClassCount}"),
                        t.enrollmentCount.ToString(CultureInfo.InvariantCulture),
                        t.completedCount.ToString(CultureInfo.InvariantCulture),
                        t.completionRate.ToString("0.0", CultureInfo.InvariantCulture),
                        t.revenueSharePercent.ToString("0.0", CultureInfo.InvariantCulture),
                        t.revenue.ToString("0", CultureInfo.InvariantCulture)));
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
            var hasPayments = await _context.Payments.AsNoTracking().AnyAsync(cancellationToken);
            var enrollmentYears = await (
                from e in _context.Enrollments.AsNoTracking()
                join cc in _context.CourseClasses.AsNoTracking() on e.ClassId equals cc.Id
                where (e.Status == "active" || e.Status == "completed") && cc.Status != "cancelled"
                select e.EnrolledAt.Year
            ).Distinct().ToListAsync(cancellationToken);

            if (hasPayments)
            {
                var paymentYears = await _context.Payments
                    .AsNoTracking()
                    .Where(p => p.Status == "completed" && (p.PaidAt != null || p.CreatedAt != null))
                    .Select(p => (p.PaidAt ?? p.CreatedAt)!.Value.Year)
                    .Distinct()
                    .ToListAsync(cancellationToken);
                enrollmentYears = enrollmentYears.Concat(paymentYears).Distinct().ToList();
            }

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
                      && cc.Status != "cancelled"
                select new
                {
                    e.UserId,
                    e.Status,
                    e.EnrolledAt,
                    ClassId = cc.Id,
                    cc.Price,
                    CourseId = c.Id,
                    CourseTitle = c.Title,
                    SubjectId = c.SubjectId,
                    SubjectName = subj != null ? subj.Name : "Chưa phân môn"
                }
            ).ToListAsync(cancellationToken);

            var validEnrollments = periodEnrollments
                .Where(x => x.Status == "active" || x.Status == "completed")
                .ToList();

            var periodPayments = hasPayments
                ? await (
                    from p in _context.Payments.AsNoTracking()
                    join cc in _context.CourseClasses.AsNoTracking() on p.ClassId equals cc.Id
                    join c in _context.Courses.AsNoTracking() on cc.CourseId equals c.Id
                    where p.Status == "completed"
                          && (p.PaidAt ?? p.CreatedAt ?? DateTime.MinValue) >= rangeStart
                          && (p.PaidAt ?? p.CreatedAt ?? DateTime.MinValue) < rangeEnd
                    select new
                    {
                        p.Amount,
                        Timestamp = p.PaidAt ?? p.CreatedAt ?? DateTime.UtcNow,
                        ClassId = cc.Id,
                        CourseId = c.Id,
                        c.SubjectId
                    }
                ).ToListAsync(cancellationToken)
                : [];

            var periodRevenue = hasPayments
                ? periodPayments.Sum(x => x.Amount)
                : validEnrollments.Sum(x => x.Price);
            var completedCount = validEnrollments.Count(x => x.Status == "completed");
            var completionRate = validEnrollments.Count > 0
                ? Math.Round(completedCount * 100.0 / validEnrollments.Count, 1)
                : 0.0;

            var previousRevenue = hasPayments
                ? await _context.Payments.AsNoTracking()
                    .Where(p => p.Status == "completed"
                                && (p.PaidAt ?? p.CreatedAt ?? DateTime.MinValue) >= prevStart
                                && (p.PaidAt ?? p.CreatedAt ?? DateTime.MinValue) < prevEnd)
                    .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m
                : await (
                    from e in _context.Enrollments.AsNoTracking()
                    join cc in _context.CourseClasses.AsNoTracking() on e.ClassId equals cc.Id
                    where e.EnrolledAt >= prevStart && e.EnrolledAt < prevEnd
                          && (e.Status == "active" || e.Status == "completed")
                          && cc.Status != "cancelled"
                    select (decimal?)cc.Price
                ).SumAsync(cancellationToken) ?? 0m;

            double? revenueGrowthPercent = previousRevenue > 0
                ? Math.Round((double)((periodRevenue - previousRevenue) / previousRevenue) * 100.0, 1)
                : null;

            // Tỷ lệ học viên tái đăng ký (>= 2 lớp khác nhau hợp lệ từ trước đến cuối kỳ đang xem, loại lớp đã hủy)
            var studentClassCounts = await (
                from e in _context.Enrollments.AsNoTracking()
                join cc in _context.CourseClasses.AsNoTracking() on e.ClassId equals cc.Id
                where e.EnrolledAt < rangeEnd
                      && (e.Status == "active" || e.Status == "completed")
                      && cc.Status != "cancelled"
                group e by e.UserId into g
                select new { UserId = g.Key, Count = g.Select(x => x.ClassId).Distinct().Count() }
            ).ToListAsync(cancellationToken);

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
                    var monthRevenue = hasPayments
                        ? periodPayments.Where(x => x.Timestamp.Month == m).Sum(x => x.Amount)
                        : monthRows.Sum(x => x.Price);
                    return new
                    {
                        month = m,
                        label = $"T{m}",
                        enrollments = monthRows.Count,
                        completed = monthRows.Count(x => x.Status == "completed"),
                        revenue = monthRevenue
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
                    c.SubjectId,
                    SubjectName = subj != null ? subj.Name : "Chưa phân môn",
                    ClassCount = _context.CourseClasses.Count(cc => cc.CourseId == c.Id && cc.Status != "cancelled")
                }
            ).ToListAsync(cancellationToken);

            var enrollmentsByCourse = validEnrollments
                .GroupBy(x => x.CourseId)
                .ToDictionary(g => g.Key, g => g.ToList());
            var revenueByCourse = hasPayments
                ? periodPayments.GroupBy(x => x.CourseId).ToDictionary(g => g.Key, g => g.Sum(p => p.Amount))
                : new Dictionary<int, decimal>();

            var courseBreakdown = allCourses
                .Select(c =>
                {
                    enrollmentsByCourse.TryGetValue(c.Id, out var list);
                    var count = list?.Count ?? 0;
                    var completed = list?.Count(x => x.Status == "completed") ?? 0;
                    var revenue = hasPayments
                        ? revenueByCourse.GetValueOrDefault(c.Id)
                        : (list?.Sum(x => x.Price) ?? 0m);
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

            // 1. Thống kê doanh thu theo Môn học (100% khớp periodRevenue)
            var allSubjects = await _context.Subjects
                .AsNoTracking()
                .Select(s => new { s.Id, s.Name, s.IsActive })
                .ToListAsync(cancellationToken);

            var coursesBySubject = allCourses.GroupBy(c => c.SubjectId).ToDictionary(g => g.Key, g => g.ToList());
            var enrollmentsBySubject = validEnrollments.GroupBy(x => x.SubjectId).ToDictionary(g => g.Key, g => g.ToList());
            var revenueBySubject = hasPayments
                ? periodPayments.GroupBy(x => x.SubjectId).ToDictionary(g => g.Key, g => g.Sum(p => p.Amount))
                : new Dictionary<int, decimal>();

            var subjectBreakdown = allSubjects
                .Select(s =>
                {
                    coursesBySubject.TryGetValue(s.Id, out var subjCourses);
                    enrollmentsBySubject.TryGetValue(s.Id, out var list);
                    var count = list?.Count ?? 0;
                    var completed = list?.Count(x => x.Status == "completed") ?? 0;
                    var revenue = hasPayments
                        ? revenueBySubject.GetValueOrDefault(s.Id)
                        : (list?.Sum(x => x.Price) ?? 0m);
                    var rate = count > 0 ? Math.Round(completed * 100.0 / count, 1) : 0.0;
                    var share = periodRevenue > 0 ? Math.Round((double)(revenue / periodRevenue) * 100.0, 1) : 0.0;

                    return new SubjectBreakdownRow(
                        s.Id,
                        s.Name,
                        s.IsActive,
                        subjCourses?.Count ?? 0,
                        subjCourses?.Sum(c => c.ClassCount) ?? 0,
                        count,
                        completed,
                        rate,
                        share,
                        revenue);
                })
                .OrderByDescending(x => x.revenue)
                .ThenByDescending(x => x.enrollmentCount)
                .ThenBy(x => x.subjectName)
                .ToList();

            // 2. Thống kê doanh thu theo Giảng viên:
            // Phân bổ doanh thu từng lớp cho GV chính (main); chia đều nếu lớp có nhiều GV chính;
            // nếu lớp chỉ có trợ giảng thì chia cho trợ giảng; nếu lớp chưa gắn GV thì gom vào nhóm "Chưa phân công GV" để tổng doanh thu luôn khớp 100%.
            var allTeachers = await (
                from t in _context.Teachers.AsNoTracking()
                join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                select new
                {
                    t.Id,
                    u.FullName,
                    t.Qualification,
                    t.IsVerified
                }
            ).ToListAsync(cancellationToken);

            var classTeacherLinks = await (
                from ct in _context.ClassTeachers.AsNoTracking()
                join cc in _context.CourseClasses.AsNoTracking() on ct.ClassId equals cc.Id
                where cc.Status != "cancelled"
                select new
                {
                    ct.ClassId,
                    ct.TeacherId,
                    Role = (ct.Role ?? "main").ToLower()
                }
            ).ToListAsync(cancellationToken);

            var linksByClass = classTeacherLinks.GroupBy(x => x.ClassId).ToDictionary(g => g.Key, g => g.ToList());
            var linksByTeacher = classTeacherLinks.GroupBy(x => x.TeacherId).ToDictionary(g => g.Key, g => g.ToList());

            var revByTeacher = new Dictionary<int, decimal>();
            var enrollByTeacher = new Dictionary<int, int>();
            var compByTeacher = new Dictionary<int, int>();
            decimal unassignedRevenue = 0m;
            int unassignedEnrollments = 0;
            int unassignedCompleted = 0;

            var enrollmentsByClass = validEnrollments.GroupBy(x => x.ClassId).ToDictionary(g => g.Key, g => g.ToList());
            var paymentRevByClass = hasPayments
                ? periodPayments.GroupBy(x => x.ClassId).ToDictionary(g => g.Key, g => g.Sum(p => p.Amount))
                : new Dictionary<int, decimal>();
            var allClassIdsInPeriod = enrollmentsByClass.Keys
                .Concat(paymentRevByClass.Keys)
                .Distinct()
                .ToList();

            foreach (var classId in allClassIdsInPeriod)
            {
                enrollmentsByClass.TryGetValue(classId, out var clsList);
                var clsRevenue = hasPayments
                    ? paymentRevByClass.GetValueOrDefault(classId)
                    : (clsList?.Sum(x => x.Price) ?? 0m);
                var clsCount = clsList?.Count ?? 0;
                var clsCompleted = clsList?.Count(x => x.Status == "completed") ?? 0;

                if (!linksByClass.TryGetValue(classId, out var assigned) || assigned.Count == 0)
                {
                    unassignedRevenue += clsRevenue;
                    unassignedEnrollments += clsCount;
                    unassignedCompleted += clsCompleted;
                    continue;
                }

                // Học viên & hoàn thành theo tất cả giảng viên đứng lớp (distinct theo TeacherId)
                foreach (var tid in assigned.Select(a => a.TeacherId).Distinct())
                {
                    enrollByTeacher[tid] = enrollByTeacher.GetValueOrDefault(tid) + clsCount;
                    compByTeacher[tid] = compByTeacher.GetValueOrDefault(tid) + clsCompleted;
                }

                var mains = assigned.Where(a => a.Role == "main").Select(a => a.TeacherId).Distinct().ToList();
                var recipients = mains.Count > 0
                    ? mains
                    : assigned.Select(a => a.TeacherId).Distinct().ToList();

                var perTeacher = Math.Floor(clsRevenue / recipients.Count);
                var remainder = clsRevenue - (perTeacher * recipients.Count);

                for (var i = 0; i < recipients.Count; i++)
                {
                    var tid = recipients[i];
                    var share = perTeacher + (i == 0 ? remainder : 0m);
                    revByTeacher[tid] = revByTeacher.GetValueOrDefault(tid) + share;
                }
            }

            var teacherBreakdown = allTeachers
                .Select(t =>
                {
                    linksByTeacher.TryGetValue(t.Id, out var tLinks);
                    var assignedClasses = tLinks?.Select(x => x.ClassId).Distinct().Count() ?? 0;
                    var mainClasses = tLinks?.Where(x => x.Role == "main").Select(x => x.ClassId).Distinct().Count() ?? 0;
                    var count = enrollByTeacher.GetValueOrDefault(t.Id);
                    var completed = compByTeacher.GetValueOrDefault(t.Id);
                    var revenue = revByTeacher.GetValueOrDefault(t.Id);
                    var rate = count > 0 ? Math.Round(completed * 100.0 / count, 1) : 0.0;
                    var share = periodRevenue > 0 ? Math.Round((double)(revenue / periodRevenue) * 100.0, 1) : 0.0;

                    return new TeacherBreakdownRow(
                        t.Id,
                        t.FullName,
                        t.Qualification,
                        t.IsVerified,
                        assignedClasses,
                        mainClasses,
                        count,
                        completed,
                        rate,
                        share,
                        revenue);
                })
                .OrderByDescending(x => x.revenue)
                .ThenByDescending(x => x.enrollmentCount)
                .ThenBy(x => x.teacherName)
                .ToList();

            if (unassignedRevenue > 0 || unassignedEnrollments > 0)
            {
                var unRate = unassignedEnrollments > 0 ? Math.Round(unassignedCompleted * 100.0 / unassignedEnrollments, 1) : 0.0;
                var unShare = periodRevenue > 0 ? Math.Round((double)(unassignedRevenue / periodRevenue) * 100.0, 1) : 0.0;
                teacherBreakdown.Add(new TeacherBreakdownRow(
                    0,
                    "Lớp chưa phân công giảng viên",
                    null,
                    false,
                    0,
                    0,
                    unassignedEnrollments,
                    unassignedCompleted,
                    unRate,
                    unShare,
                    unassignedRevenue));
            }

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
                totalReviewCount,
                revenueSource = hasPayments ? "payments" : "enrollments_estimate"
            };

            return new ReportSummaryResult(selectedYear, selectedQuarter, availableYears, kpi, monthlyTrend, courseBreakdown, subjectBreakdown, teacherBreakdown);
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

        private sealed record SubjectBreakdownRow(
            int subjectId,
            string subjectName,
            bool isActive,
            int courseCount,
            int classCount,
            int enrollmentCount,
            int completedCount,
            double completionRate,
            double revenueSharePercent,
            decimal revenue);

        private sealed record TeacherBreakdownRow(
            int teacherId,
            string teacherName,
            string? qualification,
            bool isVerified,
            int assignedClassCount,
            int mainClassCount,
            int enrollmentCount,
            int completedCount,
            double completionRate,
            double revenueSharePercent,
            decimal revenue);

        private sealed record ReportSummaryResult(
            int SelectedYear,
            int SelectedQuarter,
            List<int> AvailableYears,
            object Kpi,
            List<object> MonthlyTrend,
            List<CourseBreakdownRow> CourseBreakdown,
            List<SubjectBreakdownRow> SubjectBreakdown,
            List<TeacherBreakdownRow> TeacherBreakdown);
    }
}
