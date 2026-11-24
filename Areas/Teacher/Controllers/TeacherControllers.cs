using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CNPM_EnglishLearning.Areas.Admin.Controllers;
using CNPM_EnglishLearning.Data;

namespace CNPM_EnglishLearning.Areas.Teacher.Controllers
{
    [Area("Teacher")]
    public class DashboardController : Controller
    {
        public IActionResult Index() => View("~/Areas/Teacher/Views/dashboard.html");
    }

    [Area("Teacher")]
    public class ClassesController : Controller
    {
        public IActionResult Index() => View("~/Areas/Teacher/Views/classes.html");
        public IActionResult Detail(int id) => Redirect($"/Teacher/class-detail.html?id={id}");
    }

    [Area("Teacher")]
    public class ScheduleController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ScheduleController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index() => View("~/Areas/Teacher/Views/schedule.html");

        [HttpGet("/Teacher/api/Schedule/Weekly")]
        public async Task<IActionResult> Weekly(int? teacherId, string? email, CancellationToken cancellationToken)
        {
            var timeSlots = await _context.TimeSlots
                .AsNoTracking()
                .Where(ts => ts.IsActive)
                .OrderBy(ts => ts.StartTime)
                .Select(ts => new
                {
                    id = ts.Id,
                    name = ts.Name,
                    startTime = ts.StartTime,
                    endTime = ts.EndTime,
                    timeRange = AdminCoreHelpers.FormatTimeRange(ts.StartTime, ts.EndTime)
                })
                .ToListAsync(cancellationToken);

            var teachers = await (
                from t in _context.Teachers.AsNoTracking()
                join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                where t.IsVerified && u.Role == "teacher" && u.IsActive
                orderby u.FullName
                select new
                {
                    id = t.Id,
                    fullName = u.FullName,
                    email = u.Email,
                    qualification = t.Qualification
                }
            ).ToListAsync(cancellationToken);

            int? effectiveTeacherId = teacherId.HasValue && teacherId.Value > 0 ? teacherId.Value : null;
            if (!teacherId.HasValue && !string.IsNullOrWhiteSpace(email))
            {
                var matched = teachers.FirstOrDefault(t =>
                    string.Equals(t.email, email.Trim(), StringComparison.OrdinalIgnoreCase));
                if (matched != null)
                {
                    effectiveTeacherId = matched.id;
                }
            }

            var systemState = await AdminCoreHelpers.DetectSystemScheduleStateAsync(_context, cancellationToken);

            var classIds = systemState.ActiveClassSchedules.Select(s => s.ClassId).Distinct().ToList();
            var classTeachers = classIds.Count == 0
                ? []
                : await (
                    from ct in _context.ClassTeachers.AsNoTracking()
                    join t in _context.Teachers.AsNoTracking() on ct.TeacherId equals t.Id
                    join u in _context.Users.AsNoTracking() on t.UserId equals u.Id
                    where classIds.Contains(ct.ClassId)
                    select new { ct.ClassId, TeacherId = t.Id, TeacherName = u.FullName, ct.Role }
                ).ToListAsync(cancellationToken);

            var teachersByClass = classTeachers
                .GroupBy(x => x.ClassId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var classDaysByClass = systemState.ActiveClassSchedules
                .GroupBy(s => s.ClassId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.DayOfWeek).Distinct().ToList());

            var items = new List<object>();

            // Chỉ hiển thị lịch của các lớp học phần đang mở (Sắp diễn ra / Đang diễn ra) được xếp vào phòng
            foreach (var cs in systemState.ActiveClassSchedules)
            {
                teachersByClass.TryGetValue(cs.ClassId, out var ctList);
                if (effectiveTeacherId.HasValue && (ctList == null || !ctList.Any(x => x.TeacherId == effectiveTeacherId.Value)))
                {
                    continue;
                }

                var mainTeacher = ctList?.FirstOrDefault(x => x.Role == "main") ?? ctList?.FirstOrDefault();
                var days = classDaysByClass.TryGetValue(cs.ClassId, out var dl) ? dl : [cs.DayOfWeek];
                var stats = AdminCoreHelpers.CalculateSessionStats(days, cs.StartDate, cs.EndDate);

                items.Add(new
                {
                    sourceType = "class",
                    classroomId = cs.ClassroomId,
                    classroomName = cs.ClassroomName,
                    classId = cs.ClassId,
                    classCode = cs.ClassCode,
                    className = cs.ClassName,
                    courseTitle = (string?)null,
                    timeSlotId = cs.TimeSlotId,
                    dayOfWeek = cs.DayOfWeek,
                    teacherId = mainTeacher?.TeacherId,
                    teacherName = mainTeacher?.TeacherName ?? "Chưa phân công",
                    startDate = (DateTime?)cs.StartDate,
                    endDate = (DateTime?)cs.EndDate,
                    sessionsPerWeek = stats.SessionsPerWeek,
                    totalWeeks = stats.TotalWeeks,
                    totalSessions = stats.TotalSessions
                });
            }

            return Ok(new
            {
                success = true,
                selectedTeacherId = effectiveTeacherId ?? 0,
                teachers,
                timeSlots,
                schedules = items
            });
        }
    }

    [Area("Teacher")]
    public class LessonsController : Controller
    {
        public IActionResult Index() => View("~/Areas/Teacher/Views/lessons.html");
    }

    [Area("Teacher")]
    public class QuestionsController : Controller
    {
        public IActionResult Index() => View("~/Areas/Teacher/Views/questions.html");
    }

    [Area("Teacher")]
    public class ExamsController : Controller
    {
        public IActionResult Index() => View("~/Areas/Teacher/Views/exams.html");
    }

    [Area("Teacher")]
    public class GradingController : Controller
    {
        public IActionResult Index() => View("~/Areas/Teacher/Views/grading.html");

    }

    [Area("Teacher")]
    public class ResultsController : Controller
    {
        public IActionResult Index() => View("~/Areas/Teacher/Views/results.html");
    }

    [Area("Teacher")]
    public class ProfileController : Controller
    {
        public IActionResult Index() => View("~/Areas/Teacher/Views/profile.html");
    }
}
