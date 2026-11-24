using System.Security.Claims;
using CNPM_EnglishLearning.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CNPM_EnglishLearning.Areas.Teacher.Controllers;

[Area("Teacher")]
[Authorize(Roles = "teacher")]
[Route("api/teacher")]
public class TeacherDataController(ApplicationDbContext db) : ControllerBase
{
    // EF Core đọc ClassSchedule.DayOfWeek theo chuẩn ISO (1=Thứ 2..6=Thứ 7, 7=CN).
    // Frontend Teacher (wwwroot/js/teacher.js) dùng chuẩn DB/JS (1=CN, 2=Thứ 2..7=Thứ 7).
    private static int ToIsoDayOfWeek(DayOfWeek dayOfWeek) =>
        dayOfWeek == DayOfWeek.Sunday ? 7 : (int)dayOfWeek;

    private static int IsoToFrontendDayOfWeek(int isoDayOfWeek) =>
        isoDayOfWeek == 7 ? 1 : (isoDayOfWeek >= 1 && isoDayOfWeek <= 6 ? isoDayOfWeek + 1 : isoDayOfWeek);

    [HttpGet("{page}")]
    public async Task<IActionResult> Get(string page, int? id, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Forbid();

        var profile = await (
            from teacher in db.Teachers
            join user in db.Users on teacher.UserId equals user.Id
            where user.Id == userId && user.IsActive && user.Role == "teacher"
            select new
            {
                teacher.Id, user.FullName, user.Email, teacher.Bio,
                teacher.Qualification, teacher.IsVerified
            }).SingleOrDefaultAsync(cancellationToken);
        if (profile is null)
            return Forbid();

        var classes = db.CourseClasses.Where(courseClass => db.ClassTeachers.Any(
            assignment => assignment.TeacherId == profile.Id && assignment.ClassId == courseClass.Id));
        var courses = db.Courses.Where(course => classes.Any(courseClass => courseClass.CourseId == course.Id));
        var lessons = db.Lessons.Where(lesson => courses.Any(course => course.Id == lesson.CourseId));
        var exams = db.Exams.Where(exam => exam.CreatedByTeacherId == profile.Id);
        var attempts = db.UserExams.Where(attempt => exams.Any(exam => exam.Id == attempt.ExamId)
            && attempt.SubmittedAt != null);
        var schedule =
            from item in db.ClassSchedules
            join courseClass in classes on item.ClassId equals courseClass.Id
            join room in db.Classrooms on item.ClassroomId equals room.Id
            join slot in db.TimeSlots on item.TimeSlotId equals slot.Id
            orderby item.DayOfWeek, slot.StartTime
            select new
            {
                courseClass.ClassCode, courseClass.ClassName, item.DayOfWeek,
                Room = room.Name, slot.StartTime, slot.EndTime,
                courseClass.StartDate, courseClass.EndDate, courseClass.Status
            };

        object data;
        switch (page)
        {
            case "dashboard":
                var today = DateTime.UtcNow.AddHours(7).Date;
                var day = ToIsoDayOfWeek(today.DayOfWeek);
                var date = DateTime.SpecifyKind(today, DateTimeKind.Utc);
                var todayScheduleRows = await schedule.Where(s => s.DayOfWeek == day && s.Status != "cancelled"
                    && s.StartDate <= date && s.EndDate >= date).ToListAsync(cancellationToken);
                data = new
                {
                    ActiveClasses = await classes.CountAsync(c => c.Status == "ongoing", cancellationToken),
                    Students = await db.Enrollments.Where(e => e.Status == "active"
                        && classes.Any(c => c.Id == e.ClassId)).Select(e => e.UserId).Distinct().CountAsync(cancellationToken),
                    Pending = await db.UserExamAnswers.CountAsync(a => (a.GradingStatus == "pending_review" || a.ScoreEarned == null)
                        && attempts.Any(t => t.Id == a.UserExamId), cancellationToken),
                    Today = todayScheduleRows.Select(s => new
                    {
                        s.ClassCode, s.ClassName,
                        DayOfWeek = IsoToFrontendDayOfWeek(s.DayOfWeek),
                        s.Room, s.StartTime, s.EndTime,
                        s.StartDate, s.EndDate, s.Status
                    }).ToList()
                };
                break;
            case "classes":
                data = await classes.OrderBy(c => c.ClassCode).Select(c => new
                {
                    c.Id, c.ClassCode, c.ClassName, c.Status, c.MaxStudents, SubjectName = c.Course!.Subject!.Name,
                    Students = db.Enrollments.Count(e => e.ClassId == c.Id && e.Status == "active")
                }).ToListAsync(cancellationToken);
                break;
            case "class-detail":
                var detail = await classes.Where(c => c.Id == id).Select(c => new
                {
                    c.Id, c.ClassCode, c.ClassName, c.StartDate, c.EndDate, c.Status
                }).SingleOrDefaultAsync(cancellationToken);
                if (detail is null)
                    return NotFound();
                data = new
                {
                    Class = detail,
                    Students = await (
                        from enrollment in db.Enrollments
                        join user in db.Users on enrollment.UserId equals user.Id
                        where enrollment.ClassId == id
                        orderby user.FullName
                        select new { user.FullName, user.Email, enrollment.Status }).ToListAsync(cancellationToken)
                };
                break;
            case "schedule":
                var scheduleRows = await schedule.ToListAsync(cancellationToken);
                data = scheduleRows.Select(s => new
                {
                    s.ClassCode, s.ClassName,
                    DayOfWeek = IsoToFrontendDayOfWeek(s.DayOfWeek),
                    s.Room, s.StartTime, s.EndTime,
                    s.StartDate, s.EndDate, s.Status
                }).ToList();
                break;
            case "lessons":
                data = Array.Empty<object>(); // Loaded by the class-scoped lessons workspace.
                break;
            case "questions":
                data = Array.Empty<object>(); // The essay bank loads by assigned course.
                break;
            case "exams":
                data = Array.Empty<object>(); // Loaded by the owner-scoped exam workspace.
                break;
            case "grading":
                // The workspace loads classes first, then the selected roster and attempt.
                data = Array.Empty<object>();
                break;
            case "results":
                data = Array.Empty<object>(); // Loaded by the class-scoped gradebook API.
                break;
            case "profile":
                data = profile;
                break;
            default:
                return NotFound();
        }

        return Ok(new { profile, data });
    }
}
