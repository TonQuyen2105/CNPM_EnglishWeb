using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using CNPM_EnglishLearning.Data;
using CNPM_EnglishLearning.Models.Entities;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CNPM_EnglishLearning.Areas.Teacher.Controllers;

[Authorize(Roles = "teacher")]
[Route("api/teacher/lessons-workspace")]
public class TeacherLessonsController(ApplicationDbContext db, IAntiforgery antiforgery) : ControllerBase
{
    private IQueryable<Course> AssignedCourses()
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);
        return db.Courses.Where(c => db.CourseClasses.Any(cl => cl.CourseId == c.Id && db.ClassTeachers.Any(ct => ct.ClassId == cl.Id
            && db.Teachers.Any(t => t.Id == ct.TeacherId && t.UserId == userId
                && db.Users.Any(u => u.Id == userId && u.IsActive && u.Role == "teacher")))));
    }

    [HttpGet]
    public async Task<IActionResult> Get(int? courseId, CancellationToken cancellationToken)
    {
        var courses = AssignedCourses();
        if (courseId == null) return Ok(await courses.OrderBy(c => c.Title).Select(c => new {
            c.Id, c.Title, c.Level, Lessons = db.Lessons.Count(l => l.CourseId == c.Id)
        }).ToListAsync(cancellationToken));
        var course = await courses.Where(c => c.Id == courseId).Select(c => new { c.Id, c.Title }).SingleOrDefaultAsync(cancellationToken);
        if (course == null) return NotFound();
        var lessons = await db.Lessons.Where(l => l.CourseId == courseId).OrderBy(l => l.OrderIndex).ThenBy(l => l.Id)
            .Select(l => new { l.Id, l.Title, l.OrderIndex, l.ContentType, l.UpdatedAt, l.CreatedAt }).ToListAsync(cancellationToken);
        return Ok(new { Course = course, Lessons = lessons, Token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detail(int id, CancellationToken cancellationToken)
    {
        var courses = AssignedCourses();
        var lesson = await db.Lessons.Where(l => l.Id == id && courses.Any(c => c.Id == l.CourseId))
            .Select(l => new { l.Id, l.CourseId, l.Title, l.OrderIndex, l.ContentType, l.Content, l.CreatedAt, l.UpdatedAt }).SingleOrDefaultAsync(cancellationToken);
        if (lesson == null) return NotFound();
        return Ok(new { Lesson = lesson, Token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });
    }

    public sealed class LessonInput
    {
        [Range(1, int.MaxValue)] public int CourseId { get; set; }
        [Required, StringLength(200)] public string Title { get; set; } = "";
        [Range(1, 100000)] public int OrderIndex { get; set; }
        [Required, RegularExpression("^(vocabulary|grammar|listening|reading|speaking|writing|quiz)$")]
        public string ContentType { get; set; } = "";
        [StringLength(100000)] public string? Content { get; set; }
        public DateTime? ExpectedUpdatedAt { get; set; }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Create([FromBody] LessonInput input, CancellationToken cancellationToken) => Save(null, input, cancellationToken);

    [HttpPut("{id:int}")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Update(int id, [FromBody] LessonInput input, CancellationToken cancellationToken) => Save(id, input, cancellationToken);

    private async Task<IActionResult> Save(int? id, LessonInput input, CancellationToken ct)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(input.Title))
            return BadRequest(new { message = "Kiểm tra tiêu đề (1–200 ký tự), thứ tự (1–100.000), loại bài và nội dung (tối đa 100.000 ký tự)." });
        if (!await AssignedCourses().AnyAsync(c => c.Id == input.CourseId, ct)) return NotFound();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // Serialize ordering checks for this course, including simultaneous creates.
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM courses WHERE id = {input.CourseId} FOR UPDATE", ct);
        Lesson? lesson = null;
        if (id != null)
        {
            lesson = await db.Lessons.SingleOrDefaultAsync(l => l.Id == id && l.CourseId == input.CourseId, ct);
            if (lesson == null) return NotFound();
            if (lesson.UpdatedAt != input.ExpectedUpdatedAt)
                return Conflict(new { message = "Bài giảng đã được sửa ở phiên khác. Hãy mở lại bài để xem nội dung mới trước khi lưu." });
        }
        if (await db.Lessons.AnyAsync(l => l.CourseId == input.CourseId && l.OrderIndex == input.OrderIndex && (id == null || l.Id != id), ct))
            return Conflict(new { message = "Thứ tự này đã được dùng trong khóa học. Vui lòng chọn số khác." });
        if (lesson == null)
        {
            lesson = new Lesson { CourseId = input.CourseId };
            db.Lessons.Add(lesson);
        }
        lesson.Title = input.Title.Trim();
        lesson.OrderIndex = input.OrderIndex;
        lesson.ContentType = input.ContentType;
        lesson.Content = input.Content?.Trim();
        lesson.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Ok(new { lesson.Id, message = id == null ? "Đã thêm bài giảng." : "Đã lưu thay đổi bài giảng." });
    }
}
