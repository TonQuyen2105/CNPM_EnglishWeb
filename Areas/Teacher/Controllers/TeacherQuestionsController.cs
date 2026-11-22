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
        return db.Questions.Where(q => (q.QuestionType == "essay" || q.QuestionType == "multiple_choice" || q.QuestionType == "fill_blank") && db.Lessons.Any(l => l.Id == q.LessonId && courses.Any(c => c.Id == l.CourseId)));
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
                q.Id, q.LessonId, q.QuestionType, q.QuestionText, q.DefaultPoints, q.CreatedAt, q.UpdatedAt,
                IsUsed = db.ExamQuestions.Any(e => e.QuestionId == q.Id) || db.UserExamAnswers.Any(a => a.QuestionId == q.Id)
            }).ToListAsync(ct);
        return Ok(new { Course = course, Lessons = lessons, Questions = rows, Token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detail(int id, CancellationToken ct)
    {
        var question = await AccessibleQuestions().Where(q => q.Id == id).Select(q => new {
            q.Id, q.LessonId, q.QuestionType, q.QuestionText, q.DefaultPoints, q.CreatedAt, q.UpdatedAt
        }).SingleOrDefaultAsync(ct);
        if (question == null) return NotFound();
        var userId = UserId;
        var exams = await (from eq in db.ExamQuestions join e in db.Exams on eq.ExamId equals e.Id
            where eq.QuestionId == id && db.Teachers.Any(t => t.Id == e.CreatedByTeacherId && t.UserId == userId)
            orderby e.Title select new { e.Id, e.Title, e.IsPublished, eq.Points }).ToListAsync(ct);
        var examCount = await db.ExamQuestions.CountAsync(eq => eq.QuestionId == id, ct);
        var submitted = await HasSubmissions(id, ct);
        return Ok(new { Question = question, Exams = exams, ExamCount = examCount, IsUsed = examCount > 0 || submitted,
            Options = await db.QuestionOptions.Where(o => o.QuestionId == id).OrderBy(o => o.Id).Select(o => new { o.OptionText, o.IsCorrect }).ToListAsync(ct),
            Answers = await db.QuestionAnswers.Where(a => a.QuestionId == id).OrderBy(a => a.Id).Select(a => new { a.AnswerText, a.IsCaseSensitive }).ToListAsync(ct),
            HasSubmissions = submitted, Token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });
    }

    public sealed class QuestionInput : IValidatableObject
    {
        [Range(1, int.MaxValue)] public int CourseId { get; set; }
        [Range(1, int.MaxValue)] public int LessonId { get; set; }
        [Required, StringLength(20000)] public string QuestionText { get; set; } = "";
        [Required, RegularExpression("^(essay|multiple_choice|fill_blank)$")]
        public string QuestionType { get; set; } = "essay";
        public List<OptionInput>? Options { get; set; } = [];
        public List<AnswerInput>? Answers { get; set; } = [];
        public decimal DefaultPoints { get; set; }
        public DateTime? ExpectedUpdatedAt { get; set; }
        public IEnumerable<ValidationResult> Validate(ValidationContext context)
        {
            var options = Options ?? [];
            var answers = Answers ?? [];
            if (QuestionType == "multiple_choice")
            {
                if (options.Count < 2 || options.Count > 6 || options.Any(o => o == null || string.IsNullOrWhiteSpace(o.OptionText) || o.OptionText.Length > 2000))
                    yield return new("Trắc nghiệm cần 2–6 lựa chọn, mỗi lựa chọn 1–2.000 ký tự.");
                else if (options.Count(o => o.IsCorrect) != 1 || options.Select(o => o.OptionText.Trim().Normalize().ToUpperInvariant()).Distinct().Count() != options.Count)
                    yield return new("Chọn đúng một đáp án đúng; các lựa chọn không được trùng nhau.");
                if (answers.Count != 0) yield return new("Trắc nghiệm không nhận đáp án điền từ.");
            }
            else if (QuestionType == "fill_blank")
            {
                if (answers.Count < 1 || answers.Count > 20 || answers.Any(a => a == null || string.IsNullOrWhiteSpace(a.AnswerText) || a.AnswerText.Length > 2000))
                    yield return new("Điền từ cần 1–20 đáp án hợp lệ, mỗi đáp án 1–2.000 ký tự.");
                else if (answers.Select(a => (a.AnswerText.Trim().Normalize(), a.IsCaseSensitive)).Distinct().Count() != answers.Count)
                    yield return new("Đáp án điền từ không được trùng nhau.");
                if (options.Count != 0) yield return new("Điền từ không nhận lựa chọn trắc nghiệm.");
            }
            else if (options.Count != 0 || answers.Count != 0) yield return new("Tự luận không nhận bộ đáp án chấm tự động.");
            if (DefaultPoints < 0.01m || DefaultPoints > 99999999.99m)
                yield return new ValidationResult("Điểm phải từ 0,01 đến 99.999.999,99.", [nameof(DefaultPoints)]);
            if (decimal.Round(DefaultPoints, 2) != DefaultPoints)
                yield return new ValidationResult("Điểm tối đa 2 chữ số thập phân.", [nameof(DefaultPoints)]);
        }
    }
    public record OptionInput(string OptionText, bool IsCorrect);
    public record AnswerInput(string AnswerText, bool IsCaseSensitive);
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
            return BadRequest(new { message = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).Where(m => !string.IsNullOrWhiteSpace(m)).DefaultIfEmpty("Kiểm tra nội dung, loại câu, điểm và đáp án.")) });
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
        var question = !copy && original != null ? original : new Question();
        if (question.Id == 0) db.Questions.Add(question);
        if (question.Id != 0)
        {
            if (await db.UserExamAnswers.AnyAsync(a => db.QuestionOptions.Any(o => o.QuestionId == question.Id && o.Id == a.SelectedOptionId), ct))
                return Conflict(new { message = "Có bài làm tham chiếu lựa chọn của câu này. Không thể sửa." });
            db.QuestionOptions.RemoveRange(await db.QuestionOptions.Where(o => o.QuestionId == question.Id).ToListAsync(ct));
            db.QuestionAnswers.RemoveRange(await db.QuestionAnswers.Where(a => a.QuestionId == question.Id).ToListAsync(ct));
        }
        question.QuestionType = input.QuestionType;
        question.LessonId = input.LessonId;
        question.QuestionText = input.QuestionText.Trim();
        question.DefaultPoints = input.DefaultPoints;
        question.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        foreach (var option in input.Options ?? [])
            db.QuestionOptions.Add(new QuestionOption { QuestionId = question.Id, OptionText = option.OptionText.Trim(), IsCorrect = option.IsCorrect });
        foreach (var answer in input.Answers ?? [])
            db.QuestionAnswers.Add(new QuestionAnswer { QuestionId = question.Id, AnswerText = answer.AnswerText.Trim(), IsCaseSensitive = answer.IsCaseSensitive });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Ok(new { question.Id, message = copy ? "Đã tạo bản sao độc lập." : id == null ? "Đã thêm câu hỏi." : "Đã lưu câu hỏi." });
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
        if (await db.UserExamAnswers.AnyAsync(a => db.QuestionOptions.Any(o => o.QuestionId == id && o.Id == a.SelectedOptionId), ct))
            return Conflict(new { message = "Có bài làm tham chiếu lựa chọn của câu này. Không thể xóa." });
        db.QuestionOptions.RemoveRange(await db.QuestionOptions.Where(o => o.QuestionId == id).ToListAsync(ct));
        db.QuestionAnswers.RemoveRange(await db.QuestionAnswers.Where(a => a.QuestionId == id).ToListAsync(ct));
        await db.SaveChangesAsync(ct);
        db.Questions.Remove(question);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Ok(new { message = "Đã xóa câu hỏi chưa sử dụng." });
    }
}
