using System.Security.Claims;
using CNPM_EnglishLearning.Data;
using CNPM_EnglishLearning.Models.Entities;
using CNPM_EnglishLearning.Areas.Teacher.ViewModels;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CNPM_EnglishLearning.Areas.Teacher.Controllers;

[Authorize(Roles = "teacher")]
[Route("api/teacher/exams-workspace")]
public class TeacherExamsController(ApplicationDbContext db, IAntiforgery antiforgery) : ControllerBase
{
    private int UserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    private IQueryable<Course> AssignedCourses()
    {
        var userId = UserId;
        return db.Courses.Where(c => db.CourseClasses.Any(cl => cl.CourseId == c.Id && db.ClassTeachers.Any(ct => ct.ClassId == cl.Id
            && db.Teachers.Any(t => t.Id == ct.TeacherId && t.UserId == userId
                && db.Users.Any(u => u.Id == userId && u.IsActive && u.Role == "teacher")))));
    }
    private IQueryable<Exam> OwnExams()
    {
        var courses = AssignedCourses(); var userId = UserId;
        return db.Exams.Where(e => courses.Any(c => c.Id == e.CourseId)
            && db.Teachers.Any(t => t.Id == e.CreatedByTeacherId && t.UserId == userId));
    }
    private string? Token() => antiforgery.GetAndStoreTokens(HttpContext).RequestToken;
    private ObjectResult ConflictMessage(string message) => Conflict(new { message });

    [HttpGet]
    public async Task<IActionResult> List(int? courseId, CancellationToken ct)
    {
        var courses = await AssignedCourses().OrderBy(c => c.Title).Select(c => new { c.Id, c.Title }).ToListAsync(ct);
        if (courseId != null && !courses.Any(c => c.Id == courseId)) return NotFound();
        var exams = await OwnExams().Where(e => courseId == null || e.CourseId == courseId).OrderByDescending(e => e.Id)
            .Select(e => new { e.Id, e.CourseId, e.Title, e.DurationMinutes, e.IsPublished, e.PassScore, e.MaxAttempts, e.UpdatedAt,
                Questions = db.ExamQuestions.Count(q => q.ExamId == e.Id),
                TotalPoints = db.ExamQuestions.Where(q => q.ExamId == e.Id).Sum(q => (decimal?)q.Points) ?? 0,
                Submissions = db.UserExams.Count(a => a.ExamId == e.Id && a.SubmittedAt != null),
                HasAttempts = db.UserExams.Any(a => a.ExamId == e.Id),
                EssayOnly = !db.ExamQuestions.Any(eq => eq.ExamId == e.Id && db.Questions.Any(q => q.Id == eq.QuestionId && q.QuestionType != "essay"))
            }).ToListAsync(ct);
        return Ok(new { Courses = courses, Exams = exams, Token = Token() });
    }

    [HttpGet("bank")]
    public async Task<IActionResult> Bank(int courseId, CancellationToken ct)
    {
        if (!await AssignedCourses().AnyAsync(c => c.Id == courseId, ct)) return NotFound();
        var lessons = await db.Lessons.Where(l => l.CourseId == courseId).OrderBy(l => l.OrderIndex).ThenBy(l => l.Id)
            .Select(l => new { l.Id, l.Title, l.OrderIndex }).ToListAsync(ct);
        var questions = await db.Questions.Where(q => q.QuestionType == "essay" && db.Lessons.Any(l => l.Id == q.LessonId && l.CourseId == courseId))
            .OrderBy(q => q.Id).Select(q => new { q.Id, q.LessonId, q.QuestionText, q.DefaultPoints, q.UpdatedAt }).ToListAsync(ct);
        return Ok(new { Lessons = lessons, Questions = questions, Token = Token() });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detail(int id, CancellationToken ct)
    {
        var exam = await OwnExams().Where(e => e.Id == id).Select(e => new { e.Id, e.CourseId, e.Title, e.Description,
            e.DurationMinutes, e.PassScore, e.MaxAttempts, e.IsPublished, e.CreatedAt, e.UpdatedAt }).SingleOrDefaultAsync(ct);
        if (exam == null) return NotFound();
        var questions = await (from eq in db.ExamQuestions join q in db.Questions on eq.QuestionId equals q.Id
            where eq.ExamId == id orderby eq.OrderIndex, eq.Id
            select new { q.Id, q.LessonId, q.QuestionText, q.QuestionType, q.UpdatedAt, eq.Points, eq.OrderIndex }).ToListAsync(ct);
        var userId = UserId;
        var classes = await db.CourseClasses.Where(c => c.CourseId == exam.CourseId && db.ClassTeachers.Any(t => t.ClassId == c.Id
            && db.Teachers.Any(teacher => teacher.Id == t.TeacherId && teacher.UserId == userId)))
            .OrderBy(c => c.ClassCode).Select(c => new { c.Id, c.ClassCode, c.ClassName }).ToListAsync(ct);
        return Ok(new { Exam = exam, Questions = questions, Classes = classes,
            HasAttempts = await db.UserExams.AnyAsync(a => a.ExamId == id, ct), Token = Token() });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Create([FromBody] TeacherExamInput input, CancellationToken ct) => Save(null, input, ct);
    [HttpPut("{id:int}"), ValidateAntiForgeryToken]
    public Task<IActionResult> Update(int id, [FromBody] TeacherExamInput input, CancellationToken ct) => Save(id, input, ct);

    private async Task<IActionResult> Save(int? id, TeacherExamInput input, CancellationToken ct)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(input.Title) || input.Questions == null)
            return BadRequest(new { message = "Kiểm tra tên đề, thời gian, điểm đạt, số lượt làm và điểm từng câu. Câu hỏi không được trùng." });
        if (!await AssignedCourses().AnyAsync(c => c.Id == input.CourseId, ct)) return NotFound();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        Exam? exam = null;
        if (id != null)
        {
            if (!await OwnExams().AnyAsync(e => e.Id == id, ct)) return NotFound();
            await LockExam(id.Value, ct);
            exam = await OwnExams().SingleOrDefaultAsync(e => e.Id == id, ct);
            if (exam == null) return NotFound();
            if (exam.IsPublished || await db.UserExams.AnyAsync(a => a.ExamId == id, ct))
                return ConflictMessage("Đề đã công bố hoặc đã có lượt làm. Hãy sao chép thành đề mới để chỉnh sửa.");
            if (await db.ExamQuestions.AnyAsync(eq => eq.ExamId == id && db.Questions.Any(q => q.Id == eq.QuestionId && q.QuestionType != "essay"), ct))
                return ConflictMessage("Đề hỗn hợp hiện chỉ được xem. Hãy tạo đề tự luận mới.");
            if (exam.UpdatedAt != input.ExpectedUpdatedAt) return ConflictMessage("Đề đã thay đổi ở phiên khác. Mở lại đề trước khi lưu.");
            if (exam.CourseId != input.CourseId) return BadRequest(new { message = "Không đổi khóa học của đề đã tạo. Hãy tạo đề mới trong khóa phù hợp." });
        }
        // Deterministic FOR UPDATE locks serialize against question-bank edits/deletes.
        foreach (var item in input.Questions.OrderBy(q => q.QuestionId))
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM questions WHERE id = {item.QuestionId} FOR UPDATE", ct);
            var question = await db.Questions.SingleOrDefaultAsync(q => q.Id == item.QuestionId && q.QuestionType == "essay"
                && db.Lessons.Any(l => l.Id == q.LessonId && l.CourseId == input.CourseId), ct);
            if (question == null) return BadRequest(new { message = "Có câu hỏi không còn tồn tại hoặc không phải câu tự luận của khóa đã chọn." });
            if (question.UpdatedAt != item.ExpectedUpdatedAt)
                return ConflictMessage("Nội dung câu #" + item.QuestionId + " vừa thay đổi. Mở lại trình soạn đề để kiểm tra nội dung mới.");
        }
        if (exam == null)
        {
            var userId = UserId;
            var teacherId = await db.Teachers.Where(t => t.UserId == userId).Select(t => t.Id).SingleAsync(ct);
            exam = new Exam { CourseId = input.CourseId, CreatedByTeacherId = teacherId, IsPublished = false,
                Title = input.Title.Trim(), Description = input.Description?.Trim(), DurationMinutes = input.DurationMinutes,
                PassScore = input.PassScore, MaxAttempts = input.MaxAttempts };
            db.Exams.Add(exam);
            await db.SaveChangesAsync(ct);
        }
        var links = await db.ExamQuestions.Where(q => q.ExamId == exam.Id).ToListAsync(ct);
        db.ExamQuestions.RemoveRange(links);
        await db.SaveChangesAsync(ct); // Release old order/uniqueness entries before rebuilding.
        exam.Title = input.Title.Trim(); exam.Description = input.Description?.Trim();
        exam.DurationMinutes = input.DurationMinutes; exam.PassScore = input.PassScore; exam.MaxAttempts = input.MaxAttempts;
        exam.UpdatedAt = DateTime.UtcNow;
        for (var i = 0; i < input.Questions.Count; i++)
            db.ExamQuestions.Add(new ExamQuestion { ExamId = exam.Id, QuestionId = input.Questions[i].QuestionId, Points = input.Questions[i].Points, OrderIndex = i + 1 });
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return Ok(new { exam.Id, message = "Đã lưu bản nháp. Đề chưa được công bố." });
    }
    private Task<int> LockExam(int id, CancellationToken ct) => db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM exams WHERE id = {id} FOR UPDATE", ct);

    [HttpPost("{id:int}/publish"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id, [FromBody] TeacherExamVersion input, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        if (!await OwnExams().AnyAsync(e => e.Id == id, ct)) return NotFound();
        await LockExam(id, ct);
        var exam = await OwnExams().SingleOrDefaultAsync(e => e.Id == id, ct);
        if (exam == null) return NotFound();
        if (exam.IsPublished || exam.UpdatedAt != input.ExpectedUpdatedAt || await db.UserExams.AnyAsync(a => a.ExamId == id, ct))
            return ConflictMessage("Đề đã thay đổi, đã công bố hoặc có lượt làm. Vui lòng mở lại để kiểm tra.");
        var questions = await (from eq in db.ExamQuestions join q in db.Questions on eq.QuestionId equals q.Id
            where eq.ExamId == id select new { q.Id, q.QuestionType, q.LessonId, eq.Points }).ToListAsync(ct);
        var validInput = new TeacherExamInput { CourseId = exam.CourseId ?? 0, Title = exam.Title, Description = exam.Description,
            DurationMinutes = exam.DurationMinutes, PassScore = exam.PassScore, MaxAttempts = exam.MaxAttempts,
            Questions = questions.Select(q => new TeacherExamQuestionInput(q.Id, q.Points, null)).ToList() };
        if (!validInput.CanPublish() || questions.Any(q => q.QuestionType != "essay"))
            return BadRequest(new { message = "Đề cần ít nhất một câu tự luận, cấu hình hợp lệ và điểm đạt không vượt tổng điểm." });
        foreach (var q in questions)
            if (!await db.Lessons.AnyAsync(l => l.Id == q.LessonId && l.CourseId == exam.CourseId, ct))
                return BadRequest(new { message = "Có câu hỏi không thuộc khóa học của đề." });
        exam.IsPublished = true; exam.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return Ok(new { exam.Id, message = "Đã công bố đề thi." });
    }

    [HttpPost("{id:int}/copy"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Copy(int id, [FromBody] TeacherExamVersion input, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        if (!await OwnExams().AnyAsync(e => e.Id == id, ct)) return NotFound();
        await LockExam(id, ct);
        var original = await OwnExams().SingleOrDefaultAsync(e => e.Id == id, ct);
        if (original == null) return NotFound();
        if (original.UpdatedAt != input.ExpectedUpdatedAt) return ConflictMessage("Đề vừa thay đổi. Mở lại trước khi sao chép.");
        var links = await db.ExamQuestions.Where(eq => eq.ExamId == id).OrderBy(eq => eq.OrderIndex).ToListAsync(ct);
        if (await db.ExamQuestions.AnyAsync(eq => eq.ExamId == id && db.Questions.Any(q => q.Id == eq.QuestionId && q.QuestionType != "essay"), ct))
            return BadRequest(new { message = "Phiên bản này chỉ sao chép đề tự luận. Đề hỗn hợp hiện chỉ được xem." });
        var title = "Bản sao · " + original.Title;
        var copy = new Exam { CourseId = original.CourseId, CreatedByTeacherId = original.CreatedByTeacherId,
            Title = title.Length > 200 ? title[..200] : title, Description = original.Description, DurationMinutes = original.DurationMinutes,
            PassScore = original.PassScore, MaxAttempts = original.MaxAttempts, IsPublished = false, UpdatedAt = DateTime.UtcNow };
        db.Exams.Add(copy); await db.SaveChangesAsync(ct);
        foreach (var link in links) db.ExamQuestions.Add(new ExamQuestion { ExamId = copy.Id, QuestionId = link.QuestionId, Points = link.Points, OrderIndex = link.OrderIndex });
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return Ok(new { copy.Id, message = "Đã sao chép thành bản nháp mới. Câu hỏi được dùng lại; điểm và thứ tự của đề mới được lưu riêng." });
    }

    [HttpDelete("{id:int}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, [FromBody] TeacherExamVersion input, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        if (!await OwnExams().AnyAsync(e => e.Id == id, ct)) return NotFound();
        await LockExam(id, ct);
        var exam = await OwnExams().SingleOrDefaultAsync(e => e.Id == id, ct);
        if (exam == null) return NotFound();
        if (exam.IsPublished || await db.UserExams.AnyAsync(a => a.ExamId == id, ct))
            return ConflictMessage("Chỉ xóa được đề nháp chưa có lượt làm.");
        if (exam.UpdatedAt != input.ExpectedUpdatedAt) return ConflictMessage("Đề vừa thay đổi. Mở lại trước khi xóa.");
        db.ExamQuestions.RemoveRange(await db.ExamQuestions.Where(q => q.ExamId == id).ToListAsync(ct));
        db.Exams.Remove(exam); await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return Ok(new { message = "Đã xóa bản nháp. Câu hỏi trong ngân hàng được giữ nguyên." });
    }
}
