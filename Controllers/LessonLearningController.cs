using System.Security.Claims;
using CNPM_EnglishLearning.Data;
using CNPM_EnglishLearning.Models.Entities;
using CNPM_EnglishLearning.Services.Lessons;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CNPM_EnglishLearning.Controllers;

[Authorize(Roles = "student,teacher")]
[Route("api/lessons")]
public sealed class LessonLearningController(ApplicationDbContext db, LessonFileStore files, SupabaseLessonStorage storage) : ControllerBase
{
    private IQueryable<AssignedLesson> AccessibleLessons(int? classId = null)
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);
        var teacher = User.IsInRole("teacher");
        return db.LessonsByClass().Where(l => db.Users.Any(u => u.Id == userId && u.IsActive)
            && db.CourseClasses.Any(c => c.Id == l.ClassId && (classId == null || c.Id == classId)
                && (teacher ? db.ClassTeachers.Any(ct => ct.ClassId == c.Id && db.Teachers.Any(t => t.Id == ct.TeacherId && t.UserId == userId))
                    : c.Status != "cancelled" && db.Enrollments.Any(e => e.ClassId == c.Id && e.UserId == userId && (e.Status == "active" || e.Status == "completed")))));
    }
    [HttpGet("learn")]
    public async Task<IActionResult> Learn(int? lessonId, int? classId, CancellationToken ct)
    {
        var query = AccessibleLessons(classId);
        if (lessonId != null) query = query.Where(l => l.Lesson.Id == lessonId);
        var assignment = await query.OrderBy(l => l.Lesson.CourseId).ThenBy(l => l.ClassId).ThenBy(l => l.OrderIndex).ThenBy(l => l.Lesson.Id).FirstOrDefaultAsync(ct);
        if (assignment == null) return NotFound(new { message = "Chưa có bài giảng trong lớp đã ghi danh, hoặc bạn không có quyền xem bài này." });
        var lesson = assignment.Lesson;
        var document = LessonDocument.Read(lesson.Content);
        var course = await db.Courses.Where(c => c.Id == lesson.CourseId).Select(c => new { c.Id, c.Title }).SingleAsync(ct);
        var classroom = await db.CourseClasses.Where(c => c.Id == assignment.ClassId).Select(c => new { c.Id, c.ClassName, c.ClassCode }).SingleAsync(ct);
        var contents = await AccessibleLessons(assignment.ClassId).OrderBy(l => l.OrderIndex).ThenBy(l => l.Lesson.Id)
            .Select(l => new { l.Lesson.Id, l.Lesson.Title, l.OrderIndex }).ToListAsync(ct);
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);
        var profile = await db.Users.Where(u => u.Id == userId).Select(u => new { u.FullName, u.Email }).SingleAsync(ct);
        Response.Headers.CacheControl = "no-store";
        return Ok(new { lesson.Id, lesson.Title, assignment.OrderIndex, Content = document.Text, Class = classroom, Course = course, Contents = contents, Profile = profile,
            Video = LessonDocument.View(lesson.Id, document.Video, "video"), Pdf = LessonDocument.View(lesson.Id, document.Pdf, "pdf") });
    }
    [HttpGet("{id:int}/media/{kind}")]
    public async Task<IActionResult> Media(int id, string kind, CancellationToken ct)
    {
        if (kind is not ("video" or "pdf")) return NotFound();
        var lesson = await AccessibleLessons().Where(l => l.Lesson.Id == id).Select(l => l.Lesson).FirstOrDefaultAsync(ct);
        if (lesson == null) return NotFound();
        var document = LessonDocument.Read(lesson.Content);
        var attachment = kind == "video" ? document.Video : document.Pdf;
        if (attachment == null) return NotFound();
        Response.Headers.CacheControl = "private, no-store";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        if (attachment.Bucket != null)
        {
            try { return Redirect(await storage.SignedUrlAsync(attachment, ct)); }
            catch (InvalidDataException ex) { return StatusCode(502, new { message = ex.Message }); }
        }
        var path = files.GetPath(attachment.Key);
        if (!System.IO.File.Exists(path)) return NotFound(new { message = "File bài giảng chưa có trên máy chủ này." });
        Response.Headers.CacheControl = "private, no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        if (kind == "pdf") Response.Headers["Content-Security-Policy"] = "sandbox";
        return PhysicalFile(path, attachment.ContentType, enableRangeProcessing: true);
    }
}
