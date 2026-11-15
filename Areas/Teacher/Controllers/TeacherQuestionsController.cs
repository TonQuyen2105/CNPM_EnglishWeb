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
[Route("api/teacher/questions-workspace")]
public class TeacherQuestionsController(ApplicationDbContext db, IAntiforgery antiforgery) : ControllerBase
{
    private int UserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    private IQueryable<Course> AssignedCourses()
    {
        var userId = UserId;
        return db.Courses.Where(c => db.CourseClasses.Any(cl => cl.CourseId == c.Id && db.ClassTeachers.Any(ct => ct.ClassId == cl.Id
            && db.Teachers.Any(t => t.Id == ct.TeacherId && t.UserId == userId
                && db.Users.Any(u => u.Id == userId && u.IsActive && u.Role == "teacher")))));
    }
    private IQueryable<Question> AccessibleQuestions()
    {
        var courses = AssignedCourses();
        return db.Questions.Where(q => q.QuestionType == "essay" && db.Lessons.Any(l => l.Id == q.LessonId && courses.Any(c => c.Id == l.CourseId)));
    }
    private Task<bool> IsUsed(int id, CancellationToken ct) => db.ExamQuestions.AnyAsync(e => e.QuestionId == id, ct);
    private Task<bool> HasSubmissions(int id, CancellationToken ct) => db.UserExamAnswers.AnyAsync(a => a.QuestionId == id, ct);

    [HttpGet]
    public async Task<IActionResult> Get(int? courseId, CancellationToken ct)
    {
        var courses = AssignedCourses();
        var questions = AccessibleQuestions();
        if (courseId == null) return Ok(await courses.OrderBy(c => c.Title).Select(c => new {
            c.Id, c.Title, Questions = questions.Count(q => db.Lessons.Any(l => l.Id == q.LessonId && l.CourseId == c.Id))
        }).ToListAsync(ct));
        var course = await courses.Where(c => c.Id == courseId).Select(c => new { c.Id, c.Title }).SingleOrDefaultAsync(ct);
        if (course == null) return NotFound();
        var lessons = await db.Lessons.Where(l => l.CourseId == courseId).OrderBy(l => l.OrderIndex).ThenBy(l => l.Id)
            .Select(l => new { l.Id, l.Title, l.OrderIndex }).ToListAsync(ct);
        var rows = await questions.Where(q => db.Lessons.Any(l => l.Id == q.LessonId && l.CourseId == courseId))
            .OrderByDescending(q => q.Id).Select(q => new {
                q.Id, q.LessonId, q.QuestionText, q.DefaultPoints, q.CreatedAt, q.UpdatedAt,
                IsUsed = db.ExamQuestions.Any(e => e.QuestionId == q.Id) || db.UserExamAnswers.Any(a => a.QuestionId == q.Id)
            }).ToListAsync(ct);
        return Ok(new { Course = course, Lessons = lessons, Questions = rows, Token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detail(int id, CancellationToken ct)
    {
        var question = await AccessibleQuestions().Where(q => q.Id == id).Select(q => new {
            q.Id, q.LessonId, q.QuestionText, q.DefaultPoints, q.CreatedAt, q.UpdatedAt
        }).SingleOrDefaultAsync(ct);
        if (question == null) return NotFound();
        var userId = UserId;
        var exams = await (from eq in db.ExamQuestions join e in db.Exams on eq.ExamId equals e.Id
            where eq.QuestionId == id && db.Teachers.Any(t => t.Id == e.CreatedByTeacherId && t.UserId == userId)
            orderby e.Title select new { e.Id, e.Title, e.IsPublished, eq.Points }).ToListAsync(ct);
        var examCount = await db.ExamQuestions.CountAsync(eq => eq.QuestionId == id, ct);
        var submitted = await HasSubmissions(id, ct);
        return Ok(new { Question = question, Exams = exams, ExamCount = examCount, IsUsed = examCount > 0 || submitted,
            HasSubmissions = submitted, Token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });
    }

    public sealed class QuestionInput : IValidatableObject
    {
        [Range(1, int.MaxValue)] public int CourseId { get; set; }
        [Range(1, int.MaxValue)] public int LessonId { get; set; }
        [Required, StringLength(20000)] public string QuestionText { get; set; } = "";
        public decimal DefaultPoints { get; set; }
        public DateTime? ExpectedUpdatedAt { get; set; }
        public IEnumerable<ValidationResult> Validate(ValidationContext context)
        {
            if (DefaultPoints < 0.01m || DefaultPoints > 99999999.99m)
                yield return new ValidationResult("Điểm phải từ 0,01 đến 99.999.999,99.", [nameof(DefaultPoints)]);
            if (decimal.Round(DefaultPoints, 2) != DefaultPoints)
                yield return new ValidationResult("Điểm tối đa 2 chữ số thập phân.", [nameof(DefaultPoints)]);
        }
    }
    public record VersionInput(DateTime? ExpectedUpdatedAt);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Create([FromBody] QuestionInput input, CancellationToken ct) => Save(null, false, input, ct);
    [HttpPut("{id:int}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Update(int id, [FromBody] QuestionInput input, CancellationToken ct) => Save(id, false, input, ct);
    [HttpPost("{id:int}/copy"), ValidateAntiForgeryToken]
    public Task<IActionResult> Copy(int id, [FromBody] QuestionInput input, CancellationToken ct) => Save(id, true, input, ct);

    private async Task<IActionResult> Save(int? id, bool copy, QuestionInput input, CancellationToken ct)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(input.QuestionText))
            return BadRequest(new { message = "Chọn bài giảng, nhập nội dung (tối đa 20.000 ký tự) và điểm lớn hơn 0, tối đa 2 chữ số thập phân." });
        var courses = AssignedCourses();
        if (!await db.Lessons.AnyAsync(l => l.Id == input.LessonId && l.CourseId == input.CourseId && courses.Any(c => c.Id == l.CourseId), ct)) return NotFound();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        Question? original = null;
        if (id != null)
        {
            if (!await AccessibleQuestions().AnyAsync(q => q.Id == id, ct)) return NotFound();
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM questions WHERE id = {id.Value} FOR UPDATE", ct);
            original = await AccessibleQuestions().SingleOrDefaultAsync(q => q.Id == id, ct);
            if (original == null) return NotFound();
            if (!copy)
            {
                if (await IsUsed(id.Value, ct) || await HasSubmissions(id.Value, ct))
                    return Conflict(new { message = "Câu hỏi đã được sử dụng. Hãy tạo bản sao để chỉnh sửa mà không ảnh hưởng đề và bài nộp cũ." });
                if (original.UpdatedAt != input.ExpectedUpdatedAt)
                    return Conflict(new { message = "Câu hỏi đã được sửa ở phiên khác. Hãy mở lại câu hỏi trước khi lưu." });
            }
        }
        var question = !copy && original != null ? original : new Question { QuestionType = "essay" };
        if (question.Id == 0) db.Questions.Add(question);
        question.LessonId = input.LessonId;
        question.QuestionText = input.QuestionText.Trim();
        question.DefaultPoints = input.DefaultPoints;
        question.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Ok(new { question.Id, message = copy ? "Đã tạo bản sao độc lập." : id == null ? "Đã thêm câu hỏi tự luận." : "Đã lưu câu hỏi." });
    }

    [HttpDelete("{id:int}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, [FromBody] VersionInput input, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(new { message = "Yêu cầu xóa không hợp lệ." });
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        if (!await AccessibleQuestions().AnyAsync(q => q.Id == id, ct)) return NotFound();
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM questions WHERE id = {id} FOR UPDATE", ct);
        var question = await AccessibleQuestions().SingleOrDefaultAsync(q => q.Id == id, ct);
        if (question == null) return NotFound();
        if (await IsUsed(id, ct) || await HasSubmissions(id, ct))
            return Conflict(new { message = "Không thể xóa câu đã có trong đề thi hoặc bài nộp." });
        if (question.UpdatedAt != input.ExpectedUpdatedAt)
            return Conflict(new { message = "Câu hỏi vừa được thay đổi. Hãy mở lại để kiểm tra trước khi xóa." });
        if (await db.QuestionOptions.AnyAsync(o => o.QuestionId == id, ct) || await db.QuestionAnswers.AnyAsync(a => a.QuestionId == id, ct))
            return Conflict(new { message = "Câu hỏi có dữ liệu đáp án liên quan. Cần kiểm tra dữ liệu trước khi xóa." });
        db.Questions.Remove(question);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Ok(new { message = "Đã xóa câu hỏi chưa sử dụng." });
    }
}
